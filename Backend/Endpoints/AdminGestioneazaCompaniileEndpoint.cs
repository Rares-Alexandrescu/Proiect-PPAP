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
        //tre sa fac si ceva de filtrare etc!
        public static void MapAdminGestioneazaCompaniileEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin/vezi-companii", async (ClaimPrincipal admin, IConfiguration config) =>
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

            app.MapPost("/admin/add-companie", async (
                ClaimsPrincipal admin,
                IConfiguration config,
                [FromBody] Companie companieNoua) =>
            {
                var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(idString, out int idAdmin)) return Results.Unauthorized();

                var connectionString = config.GetConnectionString("DefaultConnection");

                var rolAdmin = await SecurityHelper.GetRol(idAdmin, connectionString);

                if (rolAdmin != "AdminGeneral")
                    return Results.Forbid();

                var erori = new Dictionary<string, List<string>>();

                void AdaugaEroare(string camp, string mesaj)
                {
                    if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
                    erori[camp].Add(mesaj);
                }

                if(!SecurityHelper.EsteEmailValid(companieNoua.Email))
                {
                    AdaugaEroare("email", "Email-ul nu are formatul corect!");
                }

                if(!SecurityHelper.EsteCnpValid(companieNoua.CnpAdminLocal))
                {
                    AdaugaEroare("cnp", "CNP-ul trebuie sa contina fix 13 cifre!");
                }

                if (!SecurityHelper.EsteNumarTelefonValid(companieNoua.NumarTelefonAdminLocal))
                {
                    AdaugaEroare("numar_telefon", "Numarul de telefon trebuie sa contina fix 10 cifre!");
                }


                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruCnp = new DynamicParameters();

                    parametruCnp.Add("@cnp", cnpHash);
                    parametruCnp..Add("@rezultat", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                    await connection.ExecuteAsync(
                        "sp_Utilizator_Exista_CNP",
                        parametruCnp,
                        commandType: CommandType.StoredProcedure
                    );

                    int rezultatCnp = parametruEmail.Get<int>("@rezultat");
                    Console.WriteLine("Rezultatul Email din procedura stocată este: " + rezultatSql);

                    if(rezultatCnp == 0)
                    {
                        AdaugaEroare("cnp", "CNP inexistent! Asigura-te ca utilizatorul exista!");
                    }
                }
                //trebuie aici procedura stocata ca sa se retina in db
            }).RequireAuthorization();


            app.MapGet("/admin/edit-companie/{id:int}", async(
                int id,
                ClaimsPrincipal admin,
                IConfiguration config
                ) =>
            {
                //Sa populez formularu cu datele companiei
            }).RequireAuthorization();

            app.MapPut("/admin/edit-companie/{id:int}", async (
                int id,
                ClaimsPrincipal admin,
                IConfiguration config,
                [FromBody] Companie companieEditata 
            ) =>
            {
                //Sa updatez intrarea pusa aici
            }).RequireAuthorization();
        }
    }
}