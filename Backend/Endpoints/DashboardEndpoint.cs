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


                    string? rolUtilizator = await connection.QueryFirstOrDefaultAsync<string>(
                        "sp_Utilizator_Get_Rol",
                        parametrii,
                        commandType: CommandType.StoredProcedure
                        );

                    Companie? companieUtilizator = null;

                    if (rolUtilizator != "AdminFurnizor")
                    {
                        companieUtilizator = await connection.QueryFirstOrDefaultAsync<Companie>(
                            "sp_Utilizator_Get_Companie",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                            );
                    }
                    else
                    {
                        //SI TRE SA MODIFIC SI IN FRONTEND SA MI INTRE LA AMDIN-FURNIZOR SI NU LA ADMIN-COMPANIE
                        companieUtilizator = await connection.QueryFirstOrDefaultAsync<Companie>(
                            "sp_Utilizator_Get_Furnizor_As_Companie",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                            );
                    }

                    if (companieUtilizator != null)
                    {
                        string jsonCompanie = JsonSerializer.Serialize(companieUtilizator, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine("=== DATE COMPANIE EXTRASE DIN BAZA DE DATE ===");
                        Console.WriteLine(jsonCompanie);
                        Console.WriteLine("================================================");
                    }
                    else
                    {
                        Console.WriteLine("Compania nu a fost găsit în baza de date!");
                    }

                    return Results.Ok(new
                    {
                        Utilizator = utilizator,
                        Rol = rolUtilizator != null ? rolUtilizator.ToString() : "N/A",
                        Companie = companieUtilizator
                    });

                }
            }).RequireAuthorization();
        }
    }
}