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

            app.MapPut("/edit-account", async({FromBody} EditAccountRequest req, ClaimsPrincipal user, IConfiguration config) =>)
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                string? cmpNouCriptat = null;
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
                    cmpNouCriptat = SecurityHelpers.CripteazaCNP(req.cnpNou);
                }

                if (req.parolaVeche != null && req.parolaNoua != null) {

                    if (!Validators.EsteParolaLunga(req.parolaNoua)) AdaugaEroare("parola", "Minim 8 caractere.");
                    if (!Validators.AreParolaCaracterMare(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o majusculă.");
                    if (!Validators.AreParolaCifra(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o cifră.");

                    if (!Validators.ParoleleCoincid(req.parolaNouaConfirmare, req.parolaNoua)) AdaugaEroare("parolaConfirmare", "Parolele nu coincid!");


                
                }



            }
        }
    }
}
            