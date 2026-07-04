using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.Helpers;
using Backend.DBClasses;

namespace Backend.Endpoints
{
    public record EditAccountRequest(
        string? nume,
        string? prenume,
        string? emailNou,
        string? cnpNou,
        string? parolaVeche,
        string? parolaNoua,
        string? parolaNouaConfirmare);


    public static class EditAccountEndpoint
    {
        public static void MapEditAccountEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/edit-account", async (ClaimsPrincipal user, IConfiguration config) =>
            {
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();
                
                var connectionString = config.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@id", idUtilizatorLogat);

                    var utilizator = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                        "sp_Utilizator_getByID",
                        parametrii,
                        commandType: CommandType.StoredProcedure
                        );


                    if (utilizator == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    return Results.Ok(new
                    {
                        nume = utilizator.Nume,
                        prenume = utilizator.Prenume,
                        email = utilizator.Email,
                    });
                }
            }).RequireAuthorization();

            app.MapPut("/edit-account", async([FromBody] EditAccountRequest req, ClaimsPrincipal user, IConfiguration config) => { 

                var connectionString = config.GetConnectionString("DefaultConnection");
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                string? cnpNouCriptat = null;
                string? parolaNouaHash = null;

                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();

                var erori = new Dictionary<string, List<string>>();

                void AdaugaEroare(string camp, string mesaj)
                {
                    if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
                    erori[camp].Add(mesaj);
                }


                if (!Validators.EsteNumePrenumeValid(req.nume)) AdaugaEroare("nume", "Doar litere, spații și cratime / Adauga ceva neaparat !");

                if (!Validators.EsteNumePrenumeValid(req.prenume)) AdaugaEroare("prenume", "Doar litere, spații și cratime / Adauga ceva neaparat !");

                if (!Validators.EsteEmailValid(req.emailNou)) AdaugaEroare("email", "Format de email invalid.");

                if (!Validators.EsteCnpValid(req.cnpNou))
                {
                    AdaugaEroare("cnp", "CNP-ul trebuie să aibă exact 13 cifre.");
                }
                else
                {
                    cnpNouCriptat = !string.IsNullOrWhiteSpace(req.cnpNou) ? SecurityHelper.CripteazaCNP(req.cnpNou) : null;
                }

                if (req.parolaVeche != null && req.parolaNoua != null) {

                    if (!Validators.EsteParolaLunga(req.parolaNoua)) AdaugaEroare("parola", "Minim 8 caractere.");
                    if (!Validators.AreParolaCaracterMare(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o majusculă.");
                    if (!Validators.AreParolaCifra(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o cifră.");

                    if (!Validators.ParoleleCoincid(req.parolaNouaConfirmare, req.parolaNoua)) AdaugaEroare("parolaConfirmare", "Parolele nu coincid!");

                    using(var connection = new SqlConnection(connectionString))
                    {
                        var parametrii = new DynamicParameters();
                        parametrii.Add("@id", idUtilizatorLogat);

                        var utilizator = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                        "sp_Utilizator_getByID",
                        parametrii,
                        commandType: CommandType.StoredProcedure);


                        if (utilizator == null)
                        {
                            AdaugaEroare("eroare", "Contul nu exista / Eroare la baza de date!");
                        }
                        else if (!SecurityHelper.VerificaParola(req.parolaVeche, utilizator.parola))
                        {
                            AdaugaEroare("parolaVeche", "Parola veche nu este buna!");
                        }

                    }
                }
                else if(string.IsNullOrWhiteSpace(req.parolaVeche) && !string.IsNullOrWhiteSpace(req.parolaNoua))
                {
                    AdaugaEroare("parola", "Nu poti sa modifici parola fara sa o introduci pe cea veche!");
                }
                else if (!string.IsNullOrWhiteSpace(req.parolaVeche) && string.IsNullOrWhiteSpace(req.parolaNoua))
                {
                    Console.WriteLine("Deci a pus parola veche, dar nu a pus nimic in parola noua, ceea ce ar trebui sa insemne ca parola noua e null!");
                }

                if (erori.Count > 0)
                {
                    return Results.BadRequest(new { eroriCampuri = erori });
                }
                else
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        parolaNouaHash = !string.IsNullOrWhiteSpace(req.parolaNoua) ? SecurityHelper.CripteazaParola(req.parolaNoua) : null;


                        var parametrii = new DynamicParameters();
                        parametrii.Add("@cnp", cnpNouCriptat);
                        parametrii.Add("@nume", req.nume);
                        parametrii.Add("@prenume", req.prenume);
                        parametrii.Add("@parola", parolaNouaHash);
                        parametrii.Add("@userId", idUtilizatorLogat);
                        parametrii.Add("@email", req.emailNou);

                        await connection.ExecuteAsync(
                            "sp_Utilizator_Edit",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                    );
                    }

                    //IMI TREBUIE SI CEVA EMAIL SERVICE, MACAR SA VERIFIC DACA MERGE EDITUL CUM TREBUIE 
                    return Results.Ok(new { message = "Datele au fost salvate cu succes!" });
                }).RequireAuthorization();
        }
    }
}

            