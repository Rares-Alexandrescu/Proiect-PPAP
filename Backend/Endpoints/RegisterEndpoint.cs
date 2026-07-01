using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Endpoints
{
    public record RegisterRequest(string Nume, string Prenume, string Email, string Cnp, string Parola);

    public static class RegisterEndpoint
    {
        public static void MapRegisterEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/register", async ([FromBody] RegisterRequest req, IConfiguration config, IEmailService emailService, IDataProtectionProvider dataProtector) =>
            {
                var erori = new Dictionary<string, List<string>>();

                void AdaugaEroare(string camp, string mesaj)
                {
                    if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
                    erori[camp].Add(mesaj);
                }

                if (!Validators.EsteNumePrenumeValid(req.Nume)) AdaugaEroare("nume", "Doar litere, spații și cratime.");
                if (!Validators.EsteNumePrenumeValid(req.Prenume)) AdaugaEroare("prenume", "Doar litere, spații și cratime.");
                if (!Validators.EsteEmailValid(req.Email)) AdaugaEroare("email", "Format de email invalid.");
                if (!Validators.EsteCnpValid(req.Cnp)) AdaugaEroare("cnp", "CNP-ul trebuie să aibă exact 13 cifre.");
                if (!Validators.EsteParolaLunga(req.Parola)) AdaugaEroare("parola", "Minim 8 caractere.");
                if (!Validators.AreParolaCaracterMare(req.Parola)) AdaugaEroare("parola", "Trebuie să conțină o majusculă.");
                if (!Validators.AreParolaCifra(req.Parola)) AdaugaEroare("parola", "Trebuie să conțină o cifră.");

                if (erori.Count > 0)
                {
                    return Results.BadRequest(new { eroriCampuri = erori });
                }

                string parolaHash = SecurityHelper.CripteazaParola(req.Parola);
                string cnpHash = SecurityHelper.CripteazaCNP(req.Cnp);

                var connectionString = config.GetConnectionString("DefaultConnection");
                int rezultatSql = 0;

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        var parametrii = new DynamicParameters();
                        parametrii.Add("@rezultat", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                        parametrii.Add("@nume", req.Nume);
                        parametrii.Add("@prenume", req.Prenume);
                        parametrii.Add("@email", req.Email);
                        parametrii.Add("@cnp", cnpHash);
                        parametrii.Add("@parola", parolaHash);

                        await connection.ExecuteAsync(
                            "sp_Utilizator_Register_Site",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                        );

                        rezultatSql = parametrii.Get<int>("@rezultat");

                        if (rezultatSql == -1 || rezultatSql == -3) AdaugaEroare("email", "Acest email este deja asociat unui cont!");
                        if (rezultatSql == -2 || rezultatSql == -3) AdaugaEroare("cnp", "Acest CNP este deja înregistrat în sistem!");

                        if (erori.Count > 0) return Results.BadRequest(new { eroriCampuri = erori });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare Baza De Date: {ex.Message}");
                    return Results.BadRequest(new { message = "A apărut o eroare la salvarea datelor." });
                }

                try
                {
                    Console.WriteLine("1");
                    var protector = dataProtector.CreateProtector("VerificareCont");
                    Console.WriteLine("2");
                    string tokenSecurizat = protector.Protect(rezultatSql.ToString());
                    Console.WriteLine("3");
                    await emailService.TrimiteEmailWelcomeAsync(req.Email, req.Nume, req.Prenume, tokenSecurizat);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare la trimiterea emailului: {ex.Message}");
                }

                return Results.Ok(new { message = "Înregistrare reușită!" });
            });
        }
    }
}