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
            //sa vad de aici facturi_furnizor + facturi_furnizor_linie
            //sa vad daca le am primit
            //la iesire, sa vad daca le am prelucrate, si dupa sa le trimit odata cu factura
            //pentru comanda
            //si dupa etc
            //dar le trimitem toate odata aici la comanda
            //trebuie sa iasa cum trebuie
            //odata ce fac endpointu asta, e scurt pe doi si gata

            app.MapGet("/admin/vezi-logistica-intrare", async (ClaimsPrincipal admin, 
                IConfiguration config
                )=>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {

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

                }
            }).RequireAuthorization();
        }
    }
}