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
    public static class FurnizorFactureazaPieseEndpoint
    {
        public static void MapFurnizorFactureazaPieseEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin-furnizor/vezi-facturi",  async(
                ClaimsPrincipal adminFurnizor,
                IConfiguration config)=>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if(erori != null) return erori;

                using( var connection = new SqlConnection(connectionString) )
                {
                    var parametruAdminFurnizor = new DynamicParameters();
                    parametruAdminFurnizor.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);

                    var facturi = await connection.QueryAsync<
                        FacturiFurnizor,
                        StatisticiFactura,
                        (FacturiFurnizor Factura, StatisticiFactura Logistica)>(

                        "sp_Furnizor_GetFacturiFurnizorByFurnizorId",
                        (factura, logistica) => (factura, logistica),
                        parametruAdminFurnizor,
                        splitOn: "stadiu_logistica_factura",            
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new
                    {
                        ListaFacturi = facturi.Select(item => new
                        {
                            Factura = item.Factura,     
                            StatisticiFactura = item.Logistica   
                        }),
                        Furnizor = furnizorAdmin
                    });
                }


            }).RequireAuthorization();

            //asta nu e gata inca, tre sa vad si piesa si eu vad daor id-ul piesei
            app.MapGet("/admin-furnizor/vezi-factura/{idFactura:int}", async (
                int idFactura,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruAdminFurnizor = new DynamicParameters();
                    parametruAdminFurnizor.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);
                    parametruAdminFurnizor.Add("@idFactura", idFactura);

                    var facturaDetaliata = await connection.QueryAsync<
                        FacturiFurnizor,
                        FacturiFurnizorLinie,
                        Piese,
                        int,
                        StatisticiFactura,
                        (FacturiFurnizor Factura, FacturiFurnizorLinie detaliiFactura, Piese piesaLinie, int cantitatePiese, StatisticiFactura Logistica)>(
                        "sp_Furnizor_GetFacturaFurnizorByFurnizorId",
                        (factura, linie, piesaLinie, cantitatePiese, logistica) => (factura, linie, piesaLinie, cantitatePiese, logistica),
                        parametruAdminFurnizor,
                        splitOn: "facturi_linie_id, piese_id, cantitate_comandata, stadiu_logistica_factura",
                        commandType: CommandType.StoredProcedure
                    );

                    if(facturaDetaliata == null || !facturaDetaliata.Any())
                    {
                        return Results.BadRequest(new { message = "Nu exista factura, sau nu e factura dumneavoastra!" });
                    }

                    var statistici = facturaDetaliata.First();

                    var facturiLinii = facturaDetaliata
                    .Where(x => x.detaliiFactura != null && x.detaliiFactura.facturi_linie_id > 0)
                    .Select(x => new
                    {
                        DetaliiFactura = x.detaliiFactura,
                        Piesa = x.piesaLinie,
                        Cantitate = x.cantitatePiese
                    })
                    .ToList();

                    return Results.Ok(new
                    {
                        Factura = statistici.Factura,
                        Statistici = statistici.Logistica,
                        Linii = facturiLinii
                    });

                }
            }).RequireAuthorization();


            //la posturile astea doua trebuie sa le trimit numai cand a emis si furnizoru factura.
            app.MapPost("/admin-furnizor/trimite-comanda/{idFactura:int}", async (
                int idFactura,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config) =>

            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

            })RequireAutorization();

            app.MapPost("/admin-furnizor/trimite-linia-comanda/{idFactura:int}/{idFacturaLinie:int}", async
            (
                int idFactura,
                int idFacturaLinie,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config)=>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

            }).RequireAuthorization();

            //app.MapPost("/admin-furnizor/genereaza-facturi")
        }
        public class StatisticiFactura
        {
            public string stadiu_logistica_text { get; set; } = "Zero";
            public int linii_expediate { get; set; } = 0;
            public int linii_total { get; set; } = 0;
        }
    }
}