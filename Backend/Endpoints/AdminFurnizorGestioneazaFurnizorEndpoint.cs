using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class AdminFurnizorGestioneazaFurnizorEndpoint
    {
        public static void MapAdminFurnizorGestioneazaFurnizorEndpoint(this IEndpointRouteBuilder app)
        {
            //sa vad cum fac /admin-furnizor, sau sa fac direct din dashboard
            //si sa scriu aici, nici nu stiu bag cioaca
            //o sa fac metodele astea si vad cum le reimplementez, adevarul e ca ma gandeam la companii:
            //am un buton care ma duce direct la vezi angajati, dar pur si simplu pot sa fac un dashboard pentru admin-companii care
            //doar sa gestioneze compania. o sa vad si eu ce si cum 
            //dar mapez ca si la /admin-companie, momentan
            app.MapGet("/admin-furnizor/vezi-piese", async (
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                

            }).RequireAuthorization();
        }
}