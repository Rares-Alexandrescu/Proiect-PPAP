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
                Console.WriteLine("nu merge filtrarea, nu merge angularu");

                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();
                Console.WriteLine("nu merge filtrarea, nu merge angularu");

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

            app.MapPut("/edit-account", async ([FromBody] EditAccountRequest req, ClaimsPrincipal user, IConfiguration config) => {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                string? parolaFinala = null;
                string cnpFinal;

                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();

                var erori = new Dictionary<string, List<string>>();

                void AdaugaEroare(string camp, string mesaj)
                {
                    if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
                    erori[camp].Add(mesaj);
                }

                Utilizator utilizatorBazaDeDate;
                using (var connection = new SqlConnection(connectionString))
                {
                    var parametriiGet = new DynamicParameters();
                    parametriiGet.Add("@id", idUtilizatorLogat);

                    utilizatorBazaDeDate = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                        "sp_Utilizator_getByID", parametriiGet, commandType: CommandType.StoredProcedure);
                }

                if (utilizatorBazaDeDate == null)
                {
                    return Results.BadRequest(new { eroriCampuri = new Dictionary<string, List<string>> { { "eroare", new List<string> { "Contul nu există în baza de date!" } } } });
                }


                Console.WriteLine("Acuma incepem la erori, trebuie sa vad sa fac logica mai buna");

                if (!Validators.EsteNumePrenumeValid(req.nume ?? "")) AdaugaEroare("nume", "Doar litere, spații și cratime / Adauga ceva neaparat !");

                if (!Validators.EsteNumePrenumeValid(req.prenume ?? "")) AdaugaEroare("prenume", "Doar litere, spații și cratime / Adauga ceva neaparat !");

                if (!Validators.EsteEmailValid(req.emailNou ?? "")) AdaugaEroare("email", "Format de email invalid.");


                if (!string.IsNullOrWhiteSpace(req.cnpNou))
                {
                    if (!Validators.EsteCnpValid(req.cnpNou))
                    {
                        AdaugaEroare("cnp", "CNP-ul trebuie să aibă exact 13 cifre.");
                        cnpFinal = utilizatorBazaDeDate.Cnp;
                    }
                    else
                    {
                        cnpFinal = SecurityHelper.CripteazaCNP(req.cnpNou);
                    }
                }
                else
                {
                    cnpFinal = utilizatorBazaDeDate.Cnp;
                }

                Console.WriteLine("cnp " + cnpFinal);
                parolaFinala = utilizatorBazaDeDate.Parola;

                if (!string.IsNullOrWhiteSpace(req.parolaNoua))
                {
                    if (string.IsNullOrWhiteSpace(req.parolaVeche))
                    {
                        AdaugaEroare("parolaVeche", "Trebuie să introduci parola veche pentru a o schimba!");
                    }
                    else if (!SecurityHelper.VerificaParola(req.parolaVeche, utilizatorBazaDeDate.Parola))
                    {
                        AdaugaEroare("parolaVeche", "Parola veche este incorectă!");
                    }
                    else
                    {

                        if (!Validators.EsteParolaLunga(req.parolaNoua)) AdaugaEroare("parola", "Minim 8 caractere.");
                        if (!Validators.AreParolaCaracterMare(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o majusculă.");
                        if (!Validators.AreParolaCifra(req.parolaNoua)) AdaugaEroare("parola", "Trebuie să conțină o cifră.");
                        if (!Validators.ParoleleCoincid(req.parolaNouaConfirmare, req.parolaNoua)) AdaugaEroare("parolaConfirmare", "Parolele nu coincid!");


                        if (!erori.ContainsKey("parola") && !erori.ContainsKey("parolaConfirmare"))
                        {
                            parolaFinala = SecurityHelper.CripteazaParola(req.parolaNoua);
                        }
                    }
                }

                if (erori.Count > 0)
                {
                    Console.WriteLine("Am gasit erori");
                    return Results.BadRequest(new { eroriCampuri = erori });
                }
                else
                {
                    Console.WriteLine("Nu am gasit erori");
                    using (var connection = new SqlConnection(connectionString))
                    {
                        var parametrii = new DynamicParameters();
                        parametrii.Add("@cnp", cnpFinal);
                        parametrii.Add("@nume", req.nume);
                        parametrii.Add("@prenume", req.prenume);
                        parametrii.Add("@parola", parolaFinala);
                        parametrii.Add("@userId", idUtilizatorLogat);
                        parametrii.Add("@email", req.emailNou);

                        await connection.ExecuteAsync(
                            "sp_Utilizator_Edit",
                            parametrii,
                            commandType: CommandType.StoredProcedure
                    );
                    }
                    //IMI TREBUIE SI CEVA EMAIL SERVICE, MACAR SA VERIFIC DACA MERGE EDITUL CUM TREBUIE 
                    Console.WriteLine("Am ajuns la final");
                    return Results.Ok(new { message = "Datele au fost salvate cu succes!" });
                }
            }).RequireAuthorization();
        }
    }
}


