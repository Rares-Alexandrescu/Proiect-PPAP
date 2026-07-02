using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Endpoints
{
    public record CerereResetareRequest(string EmailSauCNP);
    public record SchimbaParolaRequest(string Token, string ParolaNoua, string ParolaConfirmare);

    public static class ResetareParolaEndpoint
    {
        public static void MapResetPasswordEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/resetare-parola", async ([FromBody] CerereResetareRequest req, IConfiguration config, IEmailService emailService, IDataProtectionProvider dataProtector) =>
            {
                string? emailPentruTrimitere = null;

                if (string.IsNullOrWhiteSpace(req.EmailSauCNP))
                {
                    return Results.BadRequest(new { message = "Email-ul / CNP-ul este obligatoriu" });
                }
                bool esteCNP = Validators.EsteCnpValid(req.EmailSauCNP);
                bool esteEmail = Validators.EsteEmailValid(req.EmailSauCNP);

                if (!(esteCNP || esteEmail) )
                {
                    return Results.BadRequest(new { message = "Nu ai introdus nici email, dar nici parola" });
                }

                var connectionString = config.GetConnectionString("DefaultConnection");
                dynamic? utilizator = null;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametre = new DynamicParameters();
                    string emailsaucnp = req.EmailSauCNP;

                    if (esteCNP)
                        emailsaucnp = SecurityHelper.CripteazaCNP(req.EmailSauCNP).Substring(0, 13);
                    
                    parametre.Add("@emailsaucnp",emailsaucnp);

                    utilizator = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "sp_Utilizator_getbyEmailsauCNP",
                        parametre,
                        commandType: CommandType.StoredProcedure);

                    if (utilizator != null)
                    {
                        Console.WriteLine("Intra si imi ia mailul de utilizator");
                        emailPentruTrimitere = utilizator.email;
                    }
                    else
                    {
                        Console.WriteLine("Cucucubau ca nu mi l ia din sql" + emailsaucnp);
                    }
                }

                if (utilizator != null && !string.IsNullOrEmpty(emailPentruTrimitere))
                {
                    Console.WriteLine("utilizatorul este " + utilizator.nume + " " + utilizator.prenume);
                    try
                    {
                        var protector = dataProtector.CreateProtector("ResetareParola").ToTimeLimitedDataProtector();
                        string tokenSecurizat = protector.Protect(emailPentruTrimitere, TimeSpan.FromMinutes(15));
                        await emailService.TrimiteEmailResetareParolaAsync(emailPentruTrimitere, utilizator.nume, utilizator.prenume, tokenSecurizat);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Eroare la generare token/trimitere email: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("NU TRECE DE TRIMIS MAILUL!!!!!!!!!");
                    if(utilizator == null)
                    {
                        Console.WriteLine("Teapa ca nu am utilizatorul");
                    }
                    if (string.IsNullOrEmpty(emailPentruTrimitere))
                    {
                        Console.WriteLine("Teapa ca nu am emailul pentru trimitere");
                    }

                    Console.WriteLine("Si avem si " + req.EmailSauCNP);
                }

                return Results.Ok(new { message = "Dacă adresa există în sistem, vei primi un email cu instrucțiunile de resetare." });
            });

            app.MapPut("/resetare-parola", async ([FromBody] SchimbaParolaRequest req, IConfiguration config, IDataProtectionProvider dataProtector) =>
            {
                var erori = new Dictionary<string, List<string>>();

                void AdaugaEroare(string camp, string mesaj)
                {
                    if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
                    erori[camp].Add(mesaj);
                }

                if (!Validators.EsteParolaLunga(req.ParolaNoua)) AdaugaEroare("parola", "Minim 8 caractere.");
                if (!Validators.AreParolaCaracterMare(req.ParolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o majusculă.");
                if (!Validators.AreParolaCifra(req.ParolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o cifră.");
                if (!Validators.ParoleleCoincid(req.ParolaConfirmare, req.ParolaNoua)) AdaugaEroare("parolaConfirmare", "Parolele nu coincid!");

                if (erori.Count > 0)
                {
                    return Results.BadRequest(new { eroriCampuri = erori });
                }

                string emailDecriptat;

                try
                {
                    var protector = dataProtector.CreateProtector("ResetareParola").ToTimeLimitedDataProtector();
                    emailDecriptat = protector.Unprotect(req.Token);
                }
                catch
                {
                    return Results.BadRequest(new { message = "Link-ul de resetare a expirat sau este invalid. Te rugăm să soliciți altul." });
                }

                string parolaHash = SecurityHelper.CripteazaParola(req.ParolaNoua);
                var connectionString = config.GetConnectionString("DefaultConnection");

                int? idUtilizator = null;

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        int rezultatSql;
                        var parametre = new DynamicParameters();
                        parametre.Add("@emailsaucnp", emailDecriptat);

                        var utilizator = await connection.QueryFirstOrDefaultAsync<dynamic>(
                            "sp_Utilizator_getbyEmailsauCNP",
                            parametre,
                            commandType: CommandType.StoredProcedure
                        );

                        if (utilizator != null)
                        {
                            idUtilizator = utilizator.id;
                        }

                        Console.WriteLine("ID-ul găsit este: " + idUtilizator);

                        if (idUtilizator == null)
                        {
                            return Results.BadRequest(new { message = "Emailul sau CNP-ul nu există în sistem." });
                        }

                        var parametrii = new DynamicParameters();
                        parametrii.Add("@parola", parolaHash);
                        parametrii.Add("@userId", idUtilizator);
                        parametrii.Add("@rezultat", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                        await connection.ExecuteAsync("sp_Utilizator_ResetParola",
                            parametrii,
                            commandType: CommandType.StoredProcedure);

                        rezultatSql = parametrii.Get<int>("@rezultat");

                        if (rezultatSql == 1)
                            Console.WriteLine("S-a updatat parola");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare DB la resetarea parolei: {ex.Message}");
                    return Results.Problem("A apărut o eroare la server. Te rugăm să încerci din nou mai târziu.");
                }

                return Results.Ok(new { message = "Parola ta a fost schimbată cu succes!" });
            });
        }
    }
}