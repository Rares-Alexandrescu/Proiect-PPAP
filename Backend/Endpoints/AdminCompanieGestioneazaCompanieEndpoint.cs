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

    public static void MapAdminCompanieGestioneazaCompanieEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin-companie/vezi-companie", async (
            ClaimsPrincipal admin,
            IConfiguration config) =>
        {
            var eroareAutentificare = await SecurityHelper.VerificaAdminCompanie(admin, config);
            var connectionString = config.GetConnectionString("DefaultConnection");

            if (eroareAutentificare != null)
            {
                return eroareAutentificare;
            }

            var (eroareAutentificare, companieLocalAdmin) = await SecurityHelper.ObtineCompanieAdminLocal(admin, connectionString);

            using (var connection = new SqlConnection(connectionString)) {

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
            var (eroareAutentificare, companieLocalAdmin) = await SecurityHelper.ObtineCompanieAdminLocal(admin, connectionString);

            var erori = new Dictionary<string, List<string>>();
            var (emailCautare, idCautare, cnpHash) = SecurityHelper.ParseazaIdentificatorCompanie(req.identificatorAngajat, erori);

            if (erori.Count > 0)
                return Results.BadRequest(new { eroriIdentificator = erori });

            using (var connection = new SqlConnection(connectionString))
            {
                string? cnpRealDinDb = null;

                var paramUtilizator = new DynamicParameters();
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

                var paramUtilizatorCNP = new DynamicParameters();
                paramUtilizatorCNP.Add("@emailsaucnp", cnpRealDinDb);

                var utilizatorDeAdaugat = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                    "sp_Utilizator_getByEmailSauCNP",
                    paramUtilizatorCNP,
                    commandType: CommandType.StoredProcedure);


                //Poate fac ceva mail ca sa confirme utlizatorii intrarea in companie?

                if (utilizatorDeAdaugat.companie_id != int.MaxValue)
                {
                    SecurityHelper.AdaugaEroare(erori, "identificator", "Utilizatorul este deja atribuit unei companii!");
                }

                if (utilizatorDeAdaugat.rol_id == 1)
                {
                    SecurityHelper.AdaugaEroare(erori, "identificator", "Nu poti sa adaugi un Admin General in companie!");
                }

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriIdentificator = erori });

                var paramUtilizatorAdaugat = new DynamicParameters();
                paramUtilizatorAdaugat.Add("@idUtilizator", utilizatorDeAdaugat.Id);
                paramUtilizatorAdaugat.Add("@idCompanie", companieLocalAdmin.Id);

                await connection.ExecuteAsync(
                    "sp_Companie_AdaugaUtilizator",
                    paramUtilizatorAdaugat,
                    commandType: CommandType.StoredProcedure
                    );
            }
        }).RequireAuthorization();

        app.MapDelete("/admin-companie/sterge-angajat/{idAngajat:int}",
            int idAngajat,
            async (ClaimsPrincipal admin,
            IConfiguration config) =>
        {

        }).RequireAuthorization();
    }
}