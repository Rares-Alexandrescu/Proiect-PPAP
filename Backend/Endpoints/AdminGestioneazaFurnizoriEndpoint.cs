using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public record AdminGestioneazaFurnizorRequest(
    string? identificatorAngajat = string.Empty,
    string? numar_telefon,
    string? email_furnizor,
    string? nume_furnizor);

    public static class AdminGestioneazaFurnizoriEndpoint(this IEndpointRouteBuilder app)
    {

        public static void MapAdminGestioneazaFurnizoriEndpoint(this IEndpointRouteBuilder app)
        {
            //Tre sa vad sa fac filtrarea pentru asta, se aplica peste toate componentele pentru adminGeneral de vezi-....
            app.MapGet("/admin/vezi-furnizorii", async(
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);

                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                using ( var connection = new SqlConnection(connectionString) )
                {
                    var furnizori = await connection.QueryAsync<Furnizor>(
                        "sp_Furnizor_GetAll",
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(furnizori);
                }

            }).RequireAuthorization();


            //AM O PROBLEMA, CA TREBUIE SA VAD CE FAC CU ID COMPANIE, HAI CA AM FACUT O SI EU DE OAIE RAU
            app.MapPost("/admin/adauga-furnizor", async(
                [FromBody] AdminGestioneazaFurnizorRequest furnizorNou,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);

                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var erori = SecurityHelper.ValideazaDateFurnizor(furnizorNou);

                if(erori != null)
                    var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(furnizorNou.identificatorAngajat, erori);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });

                //stai ca e o problema aici, sa mi aduc aminte care era si sa o rezolv cum trebuie
                //si anume ce fac cu identificatorul
                string identificator = furnizorNou.identificatorAngajat?.Trim() ?? "";

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
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin General ca Admin Furnizor al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            else if (rolUtilizator == "AdminLocal" || rolUtilizator == "UtilizatorCompanie")
                            {
                                Console.WriteLine("Chiar daca e companie id null.... probabil ceva din testare dar mai bine sa fiu sigur");
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Acest utilizator este deja atribuit unei alte companii inscrise!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            else if(rolUtilizator == "AdminFurnizor")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Acest utilizator este deja atribuit unui alt furnizor inscris!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                        }
                    }

                    var parametriiFurnizorNou = new DynamicParameters();

                    parametriiFurnizorNou.Add("@cnpAdminFurnizor", cnpRealDinDb);
                    parametriiFurnizorNou.Add("@numarTelefon", furnizorNou.numar_telefon);
                    parametriiFurnizorNou.Add("@emailFurnizor", furnizorNou.email_furnizor);
                    parametriiFurnizorNou.Add("@numeFurnizor", furnizorNou.nume_furnizor);

                    parametriiFurnizorNou.Add("@IdReturnat", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                    //MI AM LUAT TEACA RAU, TRE SA VAD CUM REZOLV FARA SA BUBUI TOT BD-UL
                    //AM REZOLVAT IN DASHBOARD, DECI E OK, O SA MA TRIMITA OK DIN FRONTEND
                    //SI O SA MI DEA LINKURILE BINE IN FRONTEND,
                    //TREBUIE DOAR SA VERIFIC DACA E ADMIN-FURNIZOR DOAR SI DOAR SI DOAR
                    //DAR ASTA CAND FAC ADMINFURNIZORGESTIONEAAFURNIZORENDPOINT, AICI E OK
                    //am rezolvat cred, trebuie sa fiu geana

                    await connection.ExecuteAsync("sp_Furnizor_AddFurnizor", 
                        parametriiFurnizorNou, 
                        commandType: CommandType.StoredProcedure);

                    int IdReturnat = parametriiFurnizorNou.Get<int>("@IdReturnat");

                    Console.WriteLine($"=== ID-ul returnat din SQL este: {idNou} ===");

                    if (idNou <= 0)
                    {
                        SecurityHelper.AdaugaEroare(erori, "mesajEroare", "Furnizorul nu a fost introdus in baza de date!");
                        return Results.BadRequest(new { eroriCampuri = erori });
                    }

                    return Results.Ok(new { message = "Furnizor adaugat cu succes!" });
                }
            }).RequireAuthorization();

            //o sa mai trebuiasca si delete si edit si am reusit cu furnizorii, tre sa ma uit in .cs de admin + 
            //dashboard si toate alea, sa nu fie o ciorba. defapt, nu e cazul de ciorba, dar tre sa vedem ce facem si noi
            //uof bou am fost cand am facut bd-ul
            //si mi trebuie si un endpoint de AdminFurnizor....

            app.MapGet("/admin/edit-furnizor/{idFurnizor:int}", async (
                int idFurnizor,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {

                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);

                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                Console.WriteLine("Id-ul furnizorului este " + idFurnizor);

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idFurnizor", idFurnizor);

                    //sa fac metoda asta in bd ---> am facut-o 
                    var furnizorDB = await connection.QueryFirstOrDefaultAsync<AdminGestioneazaFurnizorRequest>(
                        "sp_Furnizor_getByID",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (furnizorDB == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    furnizorDB.identificatorAngajat = "***";
                    return Results.Ok(new
                    {
                        furnizor = furnizorDB,
                    });

                }

            }).RequireAuthorization();

            app.MapPut("/admin/edit-furnizor/{idFurnizor:int}", async (
                int idFurnizor,
                [FromBody] AdminGestioneazaFurnizorRequest furnizorEditat,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var erori = SecurityHelper.ValideazaDateFurnizor(furnizorEditat);
                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });

                string identificator = furnizorEditat.identificatorAngajat?.Trim() ?? "";
                bool editAdmin = !string.IsNullOrWhiteSpace(identificator) && !identificator.Contains("***");
                var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(editAdmin ? identificator : "", erori);


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
                            if (userAdmin.companie_id != int.MaxValue)
                            {
                                Console.WriteLine("Asta este companie id " + userAdmin.companie_id);
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Acest utilizator este deja atribuit unei alte companii inscrise!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }

                            var rolUtilizator = await SecurityHelper.GetRol(userAdmin.Id, connectionString);
                            if (rolUtilizator == "AdminGeneral")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin General ca Admin Furnizor al unei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                            if(rolUtilizator == "AdminFurnizor")
                            {
                                SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poți atribui un Admin Furnizor ca Admin Furnizor al altei companii!");
                                return Results.BadRequest(new { eroriCampuri = erori });
                            }
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(identificator))
                    {
                        cnpRealDinDb = "STERGE_ADMIN";
                    }

                    var parametriiFurnizor = new DynamicParameters();

                    parametriiFurnizor.Add("@NumeFurnizor", furnizorEditat.nume_furnizor);
                    parametriiFurnizor.Add("@CnpAdminFurnizor", cnpRealDinDb);
                    parametriiFurnizor.Add("@Email", furnizorEditat.email_furnizor);
                    parametriiFurnizor.Add("@NumarTelefon", furnizorEditat.numar_telefon);
                    parametriiFurnizor.Add("@idFurnizor", idFurnizor);

                    await connection.ExecuteAsync(
                        "sp_Furnizor_EditFurnizor",
                         parametriiFurnizor,
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new { message = "Compania a fost editata cu succes!" });
                }


            }).RequireAuthorization();

            //CA LA COMPANII, TREBUIE DOAR SA FIU ATENT LA STERGEREA DRACU
            //IN VIITOR, SA NU FAC VREO BUBA PE AICI
            app.MapDelete("/admin/delete-furnizor/{idFurnizor:int}", async (
                int idFurnizor,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);

                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");
                var erori = new Dictionary<string, List<string>>();

                {
                    var parametru = new DynamicParameters();
                    parametru.Add("@idFurnizor", idFurnizor);

                    var FurnizorDeSters = await connection.QueryFirstOrDefaultAsync<Furnizor>(
                        "sp_Furnizor_getbyID",
                        param: parametru,
                        commandType: CommandType.StoredProcedure);

                    if (FurnizorDeSters == null)
                    {
                        SecurityHelper.AdaugaEroare(erori, "furnizor-delete", "Nu exista aceasta companie");
                        return Results.BadRequest(new { eroriCampuri = erori });
                    }

                    await connection.ExecuteAsync(
                        "sp_Furnizor_DeleteFurnizor",
                        param: parametrii,
                        commandType: CommandType.StoredProcedure);
                }

                return Results.Oknew { message = "Furnizorul a fost sters cu succes!" };
            }).RequireAuthorization();
        }