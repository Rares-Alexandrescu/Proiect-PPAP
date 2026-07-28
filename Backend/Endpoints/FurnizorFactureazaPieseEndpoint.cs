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

                using( var connection = new SqlConnection(connectionString))
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
            //app.MapPost("/admin-furnizor/trimite-comanda")

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