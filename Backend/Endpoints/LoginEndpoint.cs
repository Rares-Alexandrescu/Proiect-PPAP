using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using Backend.Helpers;
using Backend.DBClasses; 

namespace Backend.Endpoints
{
    public record LoginRequest(string Email, string Cnp, string Parola);

    public static class LoginEndpoint
    {
        public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/login", async ([FromBody] LoginRequest req, IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                string cnpCriptat = SecurityHelper.CripteazaCNP(req.Cnp.Trim());
                Console.WriteLine(cnpCriptat + " " + req.Email);
                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@email", req.Email);
                    parametrii.Add("@cnp", cnpCriptat);

                    var utilizator = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                        "sp_Utilizator_Login",
                        parametrii,
                        commandType: CommandType.StoredProcedure
                    );
                    
                    var parametriiVerificare = new DynamicParameters();
                    parametriiVerificare.Add("@cnp", cnpCriptat);
                    parametriiVerificare.Add("@rezultat", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                    await connection.ExecuteAsync(
                        "sp_Utilizator_Exista_CNP",
                        parametriiVerificare,
                        commandType: CommandType.StoredProcedure
                    );

                    int rezultatSql = parametriiVerificare.Get<int>("@rezultat");

                    Console.WriteLine("Rezultatul din procedura stocată este: " + rezultatSql);

                    if (utilizator == null)
                    {
                        return Results.BadRequest(new { message = "Parola sau Email/CNP incorect!" });
                    }

                    bool parolaEsteCorecta = SecurityHelper.VerificaParola(req.Parola, utilizator.Parola);

                    if (!parolaEsteCorecta)
                    {
                        return Results.BadRequest(new { message = "Parola sau Email/CNP incorect!" });
                    }
                    string tokenGenerat = SecurityHelper.CreareJWTLogin(utilizator, config);
                    utilizator.JWT = tokenGenerat;

                    return Results.Ok(new { utilizator.Id, utilizator.Email, utilizator.Nume, utilizator.Prenume, utilizator.JWT });
                }
            });
        }
    }
}