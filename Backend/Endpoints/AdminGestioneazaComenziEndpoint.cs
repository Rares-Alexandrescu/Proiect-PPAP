using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Endpoints
{
    public static class AdminGestioneazaComenziEndpoint
    {

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
                            Companie = grup.First().Companie
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

                    var comandaDetaliata = await connection.QueryAsync<
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
                    parametriiProcesareComanda.Add("@idComanda", idComanda);
                    parametriiProcesareComanda.Add("@idComandaPiese", idComandaPiese);

                    //fac un sp sau doua?
                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_ComandaPiese_AdminProceseazaLinia",
                        parametriiProcesareComanda,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                    {
                        return Results.BadRequest(new { message = "Piesa nu exista, nu apartine comenzii specificate, sau nu este inca receptionata (stadiu_intern != 1)." });
                    }

                    return Results.Ok(new { message = "Comanda a fost procesata in dorintele companiei!" });
                }
            }).RequireAuthorization();

            app.MapPut("/admin/trimite-comanda/{idComanda:int}/{idComandaPiese:int}", async (
                            ClaimsPrincipal admin,
                            IConfiguration config,
                            IPDFService pdfService,
                            int idComandaPiese,
                            int idComanda) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var parametriiProcesareComanda = new DynamicParameters();
                            parametriiProcesareComanda.Add("@idComanda", idComanda);
                            parametriiProcesareComanda.Add("@idComandaPiese", idComandaPiese);

                            var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                                "sp_ComandaPiese_AdminTrimiteLinia",
                                parametriiProcesareComanda,
                                transaction: transaction,
                                commandType: CommandType.StoredProcedure
                            );

                            if (randuriAfectate == 0)
                            {
                                transaction.Rollback();
                                return Results.BadRequest(new { message = "Piesa nu exista, nu apartine comenzii specificate, sau nu este inca procesata (stadiu_intern != 2)." });
                            }

                            var parametriComandaFactura = new DynamicParameters();
                            parametriComandaFactura.Add("@idComanda", idComanda);

                            var comandaTrimisaComplet = await connection.ExecuteScalarAsync<int>(
                                "sp_Comanda_VerificaStadiuInternTrimitere",
                                parametriComandaFactura,
                                transaction: transaction,
                                commandType: CommandType.StoredProcedure
                            );

                            if (comandaTrimisaComplet == 1)
                            {
                                var companieFactura = await connection.QueryFirstOrDefaultAsync<Companie>(
                                    "sp_Companie_AdminGetCompanieByComandaId",
                                    parametriComandaFactura,
                                    transaction: transaction,
                                    commandType: CommandType.StoredProcedure);

                                if (companieFactura == null)
                                {
                                    transaction.Rollback();
                                    return Results.BadRequest(new { message = "Nu s-a putut gasi compania asociata acestei comenzi." });
                                }

                                var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                                    idComanda,
                                    companieFactura.Companie_Id,
                                    connectionString!,
                                    false);

                                if (eroareComanda != null)
                                {
                                    transaction.Rollback();
                                    return eroareComanda;
                                }

                                var parametruComanda = new DynamicParameters();
                                parametruComanda.Add("@idComanda", idComanda);
                                parametruComanda.Add("@idCompanie", companieFactura.Companie_Id);

                                var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                                    "sp_Comanda_GetPieseDetaliateCompanie",
                                    (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                                    parametruComanda,
                                    transaction: transaction,
                                    splitOn: "comanda_piese_id, furnizor_id, pretPiese, TotalPretComanda",
                                    commandType: CommandType.StoredProcedure);

                                if (rezultate == null || !rezultate.Any())
                                {
                                    transaction.Rollback();
                                    return Results.BadRequest(new { message = "Comanda e goala!" });
                                }

                                decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                                var pieseFormatatePentruPdf = rezultate.Select(r => new
                                {
                                    Piesa = r.Piesa,
                                    FurnizorPiesa = r.furnizorPiesa,
                                    DetaliiComandaPiesa = r.Linie,
                                    PretPiese = r.PretPiese
                                }).ToList();

                                string calePdfFacturaComanda = await pdfService.GenereazaPdfFacturaCompanieAsync(
                                    comandaCeruta.comanda_id,
                                    companieFactura.Nume_Companie,
                                    totalGeneralComanda,
                                    pieseFormatatePentruPdf);

                                parametruComanda.Add("@path_factura_pdf", calePdfFacturaComanda);
                                parametruComanda.Add("@pret_brut", totalGeneralComanda);

                                await connection.ExecuteAsync(
                                    "sp_Factura_Companie_TrimiteComanda",
                                    parametruComanda,
                                    transaction: transaction,
                                    commandType: CommandType.StoredProcedure);

                                transaction.Commit();
                                return Results.Ok(new { message = "Comanda a fost trimisa catre companie, factura pentru comanda asta a fost generata!" });
                            }

                            transaction.Commit();
                            return Results.Ok(new { message = "Linia de comanda a fost trimisa catre companie!" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Results.BadRequest(new { message = $"A apărut o eroare critică pe server: {ex.Message}" });
                        }
                    }
                }
            }).RequireAuthorization();
        }
    }
}