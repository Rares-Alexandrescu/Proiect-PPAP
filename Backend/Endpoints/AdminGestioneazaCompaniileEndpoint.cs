using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class AdminGestioneazaCompaniileEndpoint
    {
        public static void MapAdminGestioneazaCompaniileEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin/companii", async (ClaimPrincipal admin, IConfiguration config) =>
            {
                var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(idString, out int idAdmin)) return Results.Unauthorized();

                var connectionString = config.GetConnectionString("DefaultConnection");

                var rolAdmin = await SecurityHelper.GetRol(idAdmin, connectionString);

                if (rolAdmin != "AdminGeneral")
                    return Results.Forbid();

                using (var connection = new SqlConnection(connectionString))
                {
                    var companii = await connection.QueryAsync<Companie>(
                        "sp_Companie_GetAll",
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(companii);
                }

            }).RequireAuthorization();
        }
    }
}