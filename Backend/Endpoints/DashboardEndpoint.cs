using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using System.Text.Json; 
using Backend.DBClasses;

namespace Backend.Endpoints
{
    public static class DashboardEndpoint
    {
        public static void MapDashboardEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/dashboard", async (ClaimsPrincipal user, IConfiguration config) =>
            {
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();

                var connectionString = config.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@id", idUtilizatorLogat);


                    var utilizator = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                        "sp_Utilizator_getbyID",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    var parametru = new DynamicParameters();

                    parametru.Add("@id", idUtilizatorLogat);

                    string? rolUtilizator = await connection.QueryFirstOrDefaultAsync<string>(
                        "sp_Utilizator_Get_Rol",
                        parametru,
                        commandType: CommandType.StoredProcedure
                        );

                    if (utilizator != null)
                    {
                        string jsonUtilizator = JsonSerializer.Serialize(utilizator, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine("=== DATE UTILIZATOR EXTRASE DIN BAZA DE DATE ===");
                        Console.WriteLine(jsonUtilizator);
                        Console.WriteLine("================================================");
                    }
                    else
                    {
                        Console.WriteLine("Utilizatorul nu a fost găsit în baza de date!");
                    }

                    return Results.Ok(new
                    {
                        Utilizator = utilizator,
                        Rol = rolUtilizator != null ? rolUtilizator.ToString() : "N/A"
                    });

                }
            }).RequireAuthorization();
        }
    }
}