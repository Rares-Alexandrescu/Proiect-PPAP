using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class AdminGestioneazaComenziEndpoint
    {
        //tre sa fac si ceva de filtrare etc!
        public static void MapAdminGestioneazaComenziEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin/vezi-logistica-intrare", async (ClaimsPrincipal admin, 
                IConfiguration config
                )=>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {

                    var facturiFurnizori = await connection.QueryAsync<
                        FacturiFurnizor,
                        Furnizor,
                        StatisticiFactura,
                        (FacturiFurnizor Factura, Furnizor Furnizor, StatisticiFactura Logistica) > (
                        "sp_Furnizor_AdminGeneralGetFacturiIntrare",
                        (factura, furnizor, logistica) => (factura, furnizor, logistica),
                        splitOn: "furnizor_id, stadiu_logistica_factura",
                        commandType: CommandType.StoredProcedure
                    );

                    var facturiGrupate = facturiFurnizori
                        .GroupBy(item => item.Factura.facturi_id)
                        .Select(grup => new
                        {
                            Factura = grup.First().Factura,
                            Furnizor = grup.First().Furnizor,
                            Statistici = grup.First().Logistica
                        })
                        .ToList();

                    return Results.Ok(new
                    {
                        ListaFacturiIntrare = facturiGrupate
                    });
                }
            }).RequireAuthorization();

            app.MapGet("/admin/vezi-logistica-intrare-detaliat/{idFactura:int}", async(ClaimsPrincipal admin,
                IConfiguration config,
                int idFactura) => 
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {
                     var parametri = new DynamicParameters();
                     parametri.Add("@idFactura", idFactura);

                     var facturaDetaliata = await connection.QueryAsync<
                        FacturiFurnizor,
                        Furnizor,
                        StatisticiFactura,
                        ComandaPiese,
                        Piese,
                        (FacturiFurnizor Factura, Furnizor Furnizor, StatisticiFactura Logistica, ComandaPiese Linie, Piese Piesa) > (
                        "sp_Furnizor_AdminGeneralGetFacturiIntrare",
                        (factura, furnizor, logistica, linie, piesa) => (factura, furnizor, logistica, linie, piesa),
                        parametri,
                        splitOn: "furnizor_id, stadiu_logistica_factura, comanda_piese_id, piese_id",
                        commandType: CommandType.StoredProcedure
                    );

                    var facturaGrupata = facturaDetaliata
                        .GroupBy(item => item.Factura.facturi_id)
                        .Select(grup => new
                        {
                            Factura = grup.First().Factura,
                            Furnizor = grup.First().Furnizor,
                            Statistici = grup.First().Logistica,
                            Linii = grup.Select(x => new
                            {
                                ComandaPiesa = x.Linie,
                                Piesa = x.Piesa
                            }).ToList()
                        })
                        .FirstOrDefault();

                    if (facturaGrupata == null)
                    {
                        return Results.BadRequest(new { message = "Nu exista factura specificata." });
                    }

                    return Results.Ok(new { Factura = facturaGrupata });
                }

             }).RequireAuthorization();


            app.MapPut("/admin/receptie-primire/{idFactura:int}", async (ClaimsPrincipal admin,
                IConfiguration config,
                int idFactura) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametri = new DynamicParameters();
                    parametri.Add("@idFactura", idFactura);

                    var rezultat = await connection.ExecuteScalarAsync<int>(
                        "sp_Furnizor_AdminGeneralReceptiePrimireFactura",
                        parametri,
                        commandType: CommandType.StoredProcedure
                    );

                    if (rezultat == -1)
                    {
                        return Results.BadRequest(new { message = "Nu exista factura specificata, sau nu are linii." });
                    }

                    return Results.Ok(new
                    {
                        message = rezultat == 0
                            ? "Nu exista piese noi de receptionat (toate erau deja receptionate, sau nimic nu a fost trimis de furnizor)."
                            : "Receptie confirmata."
                    });
                }
            }).RequireAuthorization();

            app.MapGet("/admin/vezi-logistica-iesire", async (ClaimsPrincipal admin,
                IConfiguration config
                ) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {

                    var comenziIesire = await connection.QueryAsync<
                        Comanda,
                        Companie,
                        (Comanda Comanda, Companie Companie)>(
                        "sp_Companie_AdminGeneralGetFacturiIesire",
                        (comanda, companie) => (comanda, companie),
                        splitOn: "companie_id",
                        commandType: CommandType.StoredProcedure
                    );

                    var comenziGrupate = comenziIesire
                        .GroupBy(item => item.Comanda.comanda_id)
                        .Select(grup => new
                        {
                            Comanda = grup.First().Comanda,
                            Companie = grup.First().Companie,
                            ComenziPieseId = grup.Select(x => x.comandaPieseId).ToList()
                        })
                        .ToList();

                    return Results.Ok(new
                    {
                        ListaComenziIesire = comenziGrupate
                    });
                }
            }).RequireAuthorization();

            app.MapGet("/admin/vezi-logistica-iesire-detaliat/{idComanda:int}", async (
                ClaimsPrincipal admin,
                IConfiguration config,
                int idComanda) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametri = new DynamicParameters();
                    parametri.Add("@idComanda", idComanda);

                    var comandaDetaliata = await connection.QueryAsync
                        Comanda,
                        Companie,
                        ComandaPiese,
                        Piese,
                        (Comanda Comanda, Companie Companie, ComandaPiese Linie, Piese Piesa)>(
                        "sp_Companie_AdminGeneralGetFacturiIesire",
                        (comanda, companie, linie, piesa) => (comanda, companie, linie, piesa),
                        parametri,
                        splitOn: "companie_id, comanda_piese_id, piese_id",
                        commandType: CommandType.StoredProcedure
                    );

                    var comandaGrupata = comandaDetaliata
                        .GroupBy(item => item.Comanda.comanda_id)
                        .Select(grup => new
                        {
                            Comanda = grup.First().Comanda,
                            Companie = grup.First().Companie,
                            Linii = grup.Select(x => new
                            {
                                ComandaPiesa = x.Linie,
                                Piesa = x.Piesa
                            }).ToList()
                        })
                        .FirstOrDefault();

                    if (comandaGrupata == null)
                    {
                        return Results.BadRequest(new { message = "Nu exista comanda specificata." });
                    }

                    return Results.Ok(new { Comanda = comandaGrupata });
                }
            }).RequireAuthorization();

            //din unu in doi, si numai din unu in doi
            app.MapPut("/admin/proceseaza-comanda/{idComanda:int}/{idComandaPiese:int}", async (
                ClaimsPrincipal admin,
                IConfiguration config,
                int idComandaPiese,
                int idComanda) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametriiProcesareComanda = new DynamicParameters();
                    parametriiProcesareComanda.Add("@idComanda", idComandaPiese);
                    parametriiProcesareComanda.Add("@idComandaPiese", idComandaPiese);

                    //fac un sp sau doua?
                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_ComandaPiese_AdminProceseazaLinia",
                        parametri,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                    {
                        return Results.BadRequest(new { message = "Piesa nu exista, nu apartine comenzii specificate, sau nu este inca receptionata (stadiu_intern != 1)." });
                    }

                    return Results.Ok(new { message = "Comanda a fost procesata in dorintele companiei!" });
                }
            }).RequireAuthorization();

            //din doi in trei, si numai din doi in trei
            app.MapPut("/admin/trimite-comanda/{idComanda:int}/{idComandaPiese:int}", async (
                ClaimsPrincipal admin,
                IConfiguration config,
                int idComandaPiese,
                int idComanda) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametriiProcesareComanda = new DynamicParameters();
                    parametriiProcesareComanda.Add("@idComanda", idComandaPiese);
                    parametriiProcesareComanda.Add("@idComandaPiese", idComandaPiese);

                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_ComandaPiese_AdminTrimiteLinia",
                        parametri,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                    {
                        return Results.BadRequest(new { message = "Piesa nu exista, nu apartine comenzii specificate, sau nu este inca procesata (stadiu_intern != 2)." });
                    }

                    return Results.Ok(new { message = "Comanda a fost trimisa catre companie!" });
                }
            }).RequireAuthorization();
        }
    }
}