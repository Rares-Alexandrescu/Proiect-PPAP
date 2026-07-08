using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace Backend.Endpoints
{
    public static class ConfirmEndpoint
    {
        public static void MapConfirmEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/confirmare-cont", async ([FromQuery] string token, IConfiguration config, IDataProtectionProvider dataProtector) =>
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.BadRequest("Link-ul de confirmare este invalid sau lipsește.");
                }

                string frontendUrl = config["Frontend:BaseUrl"] ?? "http://localhost:4200";


                int idDecriptat;

                try
                {
                    var protector = dataProtector.CreateProtector("VerificareCont").ToTimeLimitedDataProtector();
                    string idText = protector.Unprotect(token);
                    idDecriptat = int.Parse(idText);
                }
                catch
                {
                    return Results.Redirect($"{frontendUrl}/login?eroare=token_invalid");
                }

                var connectionString = config.GetConnectionString("DefaultConnection");

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        var parametrii = new DynamicParameters();
                        parametrii.Add("@id", idDecriptat);

                        await connection.ExecuteAsync(
                            "sp_Utilizator_Confirm",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare DB la confirmarea contului: {ex.Message}");
                    return Results.Problem("A apărut o eroare la server. Te rugăm să încerci din nou mai târziu.");
                }


                return Results.Redirect($"{frontendUrl}/login?confirmat=true");
            });
        }
    }
}