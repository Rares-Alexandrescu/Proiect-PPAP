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
            //Tre sa vad sa fac filtrarea pentru asta, se aplica peste toate componentele pentru adminGeneral de vezi-....
            app.MapGet("/admin/vezi-companii", async (ClaimsPrincipal admin, IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);

                Console.WriteLine("Deci intra aici");
                if (eroareAutentificare != null) return eroareAutentificare;
                Console.WriteLine("Si iese de aici");
                var connectionString = config.GetConnectionString("DefaultConnection");

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
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var erori = SecurityHelper.ValideazaDateCompanie(companieNoua);

                string identificator = companieNoua.CnpAdminLocal?.Trim() ?? "";
                var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(identificator, erori);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });

                using (var connection = new SqlConnection(connectionString))
                {

                    string? cnpRealDinDb = null;

                    if (!string.IsNullOrWhiteSpace(identificator))
                    {
                        var paramCautare = new DynamicParameters();
                        paramCautare.Add("@Email", emailCautare);
                        paramCautare.Add("@Id", idCautare);
                        paramCautare.Add("@CnpHash", cnpHash);

                        cnpRealDinDb = await connection.QueryFirstOrDefaultAsync<string>(
                                    "sp_Utilizator_GetCnpByIdentificator",
                                    paramCautare,
                                    commandType: CommandType.StoredProcedure);


                        if (string.IsNullOrEmpty(cnpRealDinDb))
                        {
                            SecurityHelper.AdaugaEroare(erori, "identificator", "Nu am găsit niciun utilizator cu aceste date!");
                            return Results.BadRequest(new { eroriCampuri = erori });
                        }

                        var paramVerificare = new DynamicParameters();
                        paramVerificare.Add("@emailsaucnp", cnpRealDinDb);

                        var userAdmin = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                                    "sp_Utilizator_getbyEmailsauCNP",
                                    paramVerificare,
                                    commandType: CommandType.StoredProcedure);

                        if (userAdmin != null)
                        {
                            if (userAdmin.companie_id != int.MaxValue)
                            {
                                Console.WriteLine("Asta este companie id " + userAdmin.companie_id);
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Acest utilizator este deja atribuit unei alte companii inscrise!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            var rolUtilizator = await SecurityHelper.GetRol(userAdmin.Id, connectionString);
                            if (rolUtilizator == "AdminGeneral")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin General ca Admin Local al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            else if (rolUtilizator == "AdminFurnizor")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin Furnizor ca Admin Local al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                        }
                    }
                    //trebuie aici procedura stocata ca sa se retina in db
                    var parametriiCompanie = new DynamicParameters();

                    parametriiCompanie.Add("@NumeCompanie", companieNoua.Nume_Companie);
                    parametriiCompanie.Add("@CnpAdminLocal", cnpRealDinDb);
                    parametriiCompanie.Add("@Email", companieNoua.Email);
                    parametriiCompanie.Add("@NumarTelefon", companieNoua.Numar_Telefon);


                    await connection.ExecuteAsync(
                        "sp_Companie_AddCompanie",
                         parametriiCompanie,
                        commandType: CommandType.StoredProcedure
                    ); //aici se atribuie utilziatorul ca si admin!

                    return Results.Ok(new { message = "Compania a fost adăugată cu succes!" });
                }
                
            }).RequireAuthorization();


            app.MapGet("/admin/edit-companie/{idCompanie:int}", async (
                int idCompanie,
                ClaimsPrincipal admin,
                IConfiguration config
                ) =>
            {
                //Sa populez formularu cu datele companiei, dar tre sa vad ce fac cu utilizatorul ala ca cnp - ul e criptat
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");
                Console.WriteLine("id-ul companiei este " + idCompanie);
                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@id", idCompanie);

                    var companieDB = await connection.QueryFirstOrDefaultAsync<Companie>(
                        "sp_Companie_getByID",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (companieDB == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    companieDB.CnpAdminLocal = "***";
                    return Results.Ok(new
                    {
                        companie = companieDB,
                    });

                }

            }).RequireAuthorization();

            app.MapPut("/admin/edit-companie/{idCompanie:int}", async (
                int idCompanie,
                ClaimsPrincipal admin,
                IConfiguration config,
                [FromBody] Companie companieEditata
            ) =>
            {

                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var erori = SecurityHelper.ValideazaDateCompanie(companieEditata);

                string identificator = companieEditata.CnpAdminLocal?.Trim() ?? "";
                bool editAdmin = !string.IsNullOrWhiteSpace(identificator) && !identificator.Contains("***");

                var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(editAdmin ? identificator : "", erori);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });


                using (var connection = new SqlConnection(connectionString))
                {
                    string? cnpRealDinDb = null;
                    if (editAdmin)
                    {
                        Console.WriteLine("Identifiactorul nostru final este unul din asta " + cnpHash + " sau " + emailCautare + " sau " + idCautare);
                        var paramCautare = new DynamicParameters();
                        paramCautare.Add("@Email", emailCautare);
                        paramCautare.Add("@Id", idCautare);
                        paramCautare.Add("@CnpHash", cnpHash);

                        cnpRealDinDb = await connection.QueryFirstOrDefaultAsync<string>(
                                    "sp_Utilizator_GetCnpByIdentificator",
                                    paramCautare,
                                    commandType: CommandType.StoredProcedure);


                        if (string.IsNullOrEmpty(cnpRealDinDb))
                        {
                            SecurityHelper.AdaugaEroare(erori, "identificator", "Nu am găsit niciun utilizator cu aceste date!");
                            return Results.BadRequest(new { eroriCampuri = erori });
                        }


                        var paramVerificare = new DynamicParameters();
                        paramVerificare.Add("@emailsaucnp", cnpRealDinDb);

                        var userAdmin = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                                    "sp_Utilizator_getbyEmailsauCNP",
                                    paramVerificare,
                                    commandType: CommandType.StoredProcedure);


                        if (userAdmin != null)
                        {
                            if (userAdmin.companie_id != int.MaxValue && userAdmin.companie_id != idCompanie)
                            {
                                Console.WriteLine("Asta este companie id " + userAdmin.companie_id);
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Acest utilizator este deja atribuit unei alte companii inscrise!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            var rolUtilizator = await SecurityHelper.GetRol(userAdmin.Id, connectionString);
                            if (rolUtilizator == "AdminGeneral")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin General ca Admin Local al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            if (rolUtilizator == "AdminFurnizor")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin Furnizor ca Admin Local al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                        }
                    }

                    else if (string.IsNullOrWhiteSpace(identificator))
                    {
                        cnpRealDinDb = "STERGE_ADMIN";
                    }

                    var parametriiCompanie = new DynamicParameters();

                    parametriiCompanie.Add("@NumeCompanie", companieEditata.Nume_Companie);
                    parametriiCompanie.Add("@CnpAdminLocal", cnpRealDinDb);
                    parametriiCompanie.Add("@Email", companieEditata.Email);
                    parametriiCompanie.Add("@NumarTelefon", companieEditata.Numar_Telefon);
                    parametriiCompanie.Add("@idCompanie", idCompanie);

                    await connection.ExecuteAsync(
                        "sp_Companie_EditCompanie",
                         parametriiCompanie,
                        commandType: CommandType.StoredProcedure
                    );
                    return Results.Ok(new { message = "Compania a fost editata cu succes!" });
                }

                
            }).RequireAuthorization();

            //si mai am de facut delete-ul, dar nu stiu cum sa l fac acuma sa mearga cat mai bine
            app.MapDelete("/admin/delete-companie/{idCompanie:int}", async (
                int idCompanie,
                ClaimsPrincipal admin,
                IConfiguration config

            ) =>
            {
                //DE MODIFICAT STERGEREA ODATA CE MAI ADAUGAM CHESTII
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var erori = new Dictionary<string, List<string>>();


                using (var connection = new SqlConnection(connectionString))
                {
                    var parametru = new DynamicParameters();
                    parametru.Add("@id", idCompanie);

                    var companieDeSters = await connection.QueryFirstOrDefaultAsync<Companie>(
                        "sp_Companie_getbyID",
                        param: parametru,
                        commandType: CommandType.StoredProcedure);

                    if (companieDeSters == null)
                    {
                        SecurityHelper.AdaugaEroare(erori, "companie-delete", "Nu exista aceasta companie");
                        return Results.BadRequest(new { eroriCampuri = erori });
                    }


                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idCompanie", idCompanie);

                    await connection.ExecuteAsync(
                        "sp_Companie_DeleteCompanie",
                        param: parametrii,
                        commandType: CommandType.StoredProcedure);
                }

                return Results.Ok {new message = "Compania a fost stearsa cu succes!" };

            }).RequireAuthorization();
        }
    }
}