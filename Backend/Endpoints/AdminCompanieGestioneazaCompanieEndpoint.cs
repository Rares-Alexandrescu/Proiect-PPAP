using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public record AdminCompanieGestioneazaCompanieRequest(
    string? identificatorAngajat);
    public static class AdminCompanieGestioneazaCompanieEndpoint
    {
        public static void MapAdminCompanieGestioneazaCompanieEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin-companie/vezi-companie", async (
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                var (eroareCompanie, companieLocalAdmin) = await SecurityHelper.ObtineCompanieAdminLocal(admin, connectionString);

                if (eroareCompanie != null) return eroareCompanie;

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametruAngajatiCompanie = new DynamicParameters();
                    parametruAngajatiCompanie.Add("@id", companieLocalAdmin.Companie_Id);

                    var angajatiCompanie = await connection.QueryAsync<Utilizator>
                    ("sp_Utilizator_getByCompanieId",
                    parametruAngajatiCompanie,
                    commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new
                    {
                        companie = companieLocalAdmin,
                        angajati = angajatiCompanie
                    });

                }
            }).RequireAuthorization();

            app.MapPost("/admin-companie/adauga-angajat",
                async (
                [FromBody] AdminCompanieGestioneazaCompanieRequest req,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;


                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroareCompanie, companieLocalAdmin) = await SecurityHelper.ObtineCompanieAdminLocal(admin, connectionString);

                if (eroareCompanie != null) return eroareCompanie;

                var erori = new Dictionary<string, List<string>>();
                var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(req.identificatorAngajat, erori);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriIdentificator = erori });

                using (var connection = new SqlConnection(connectionString))
                {
                    string? cnpRealDinDb = null;

                    var paramUtilizator = new DynamicParameters();
                    Console.WriteLine("Identifiactorul nostru final este unul din asta " + cnpHash + " sau " + emailCautare + " sau " + idCautare);
                    paramUtilizator.Add("@Email", emailCautare);
                    paramUtilizator.Add("@Id", idCautare);
                    paramUtilizator.Add("@CnpHash", cnpHash);

                    cnpRealDinDb = await connection.QueryFirstOrDefaultAsync<string>(
                        "sp_Utilizator_GetCnpByIdentificator",
                        paramUtilizator,
                        commandType: CommandType.StoredProcedure);

                    if (string.IsNullOrEmpty(cnpRealDinDb))
                    {
                        SecurityHelper.AdaugaEroare(erori, "identificator", "Nu am găsit niciun utilizator cu aceste date!");
                        return Results.BadRequest(new { eroriIdentificator = erori });
                    }
                    else
                    {
                        Console.WriteLine("Ce are, ca il gaseste??????");
                    }

                    var (eroareValidare, utilizatorDeAdaugat) = await SecurityHelper.VerificaUtilizatorPentruAdaugareInCompanie(
                        cnpRealDinDb,
                        companieLocalAdmin,
                        connectionString!);

                    if (eroareValidare != null) return eroareValidare;


                    var paramUtilizatorAdaugat = new DynamicParameters();
                    paramUtilizatorAdaugat.Add("@idUtilizator", utilizatorDeAdaugat.Id);
                    paramUtilizatorAdaugat.Add("@idCompanie", companieLocalAdmin.Companie_Id);

                    int statusAdaugare = await connection.ExecuteScalarAsync<int>(
                        "sp_Companie_AdaugaUtilizator",
                        paramUtilizatorAdaugat,
                        commandType: CommandType.StoredProcedure);

                    if (statusAdaugare > 0)
                        return Results.Ok(new { message = "Utilizatorul a fost adaugat cu succes in companie!" });
                    else
                        return Results.BadRequest(new { message = "Nu am putut adauga utilizatorul in companie cu ID-ul " + utilizatorDeAdaugat.Id });
                }

            }).RequireAuthorization();

            app.MapDelete("/admin-companie/sterge-angajat/{idAngajat:int}",
                async (int idAngajat,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (eroareCompanie, companieLocalAdmin) = await SecurityHelper.ObtineCompanieAdminLocal(admin, connectionString);

                if (eroareCompanie != null) return eroareCompanie;

                var erori = new Dictionary<string, List<string>>();

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idUtilizator", idAngajat);
                    parametrii.Add("@idCompanie", companieLocalAdmin.Companie_Id);


                    int randuriModificate = await connection.QueryFirstOrDefaultAsync<int>(
                        "sp_Companie_DeleteUtilizator",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (randuriModificate == 0)
                    {

                        SecurityHelper.AdaugaEroare(erori, "mesajEroare", "Utilizatorul nu a putut fi sters! Ori nu exista, ori nu e din compania ta!");
                        return Results.BadRequest(new { eroriIdentificator = erori });

                    }

                }

                return Results.Ok(new { message = "Utilizator a fost sters cu succes!" });

            }).RequireAuthorization();
        }
    }
}