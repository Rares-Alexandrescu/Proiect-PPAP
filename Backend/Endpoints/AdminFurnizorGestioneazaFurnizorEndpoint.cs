using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class AdminFurnizorGestioneazaFurnizorEndpoint
    {
        public static void MapAdminFurnizorGestioneazaFurnizorEndpoint(this IEndpointRouteBuilder app)
        {
            //sa vad cum fac /admin-furnizor, sau sa fac direct din dashboard
            //si sa scriu aici, nici nu stiu bag cioaca
            //o sa fac metodele astea si vad cum le reimplementez, adevarul e ca ma gandeam la companii:
            //am un buton care ma duce direct la vezi angajati, dar pur si simplu pot sa fac un dashboard pentru admin-companii care
            //doar sa gestioneze compania. o sa vad si eu ce si cum 
            //dar mapez ca si la /admin-companie, momentan
            
            //adminul furnizor ar trebui sa nu aiba acces nici macar la jsonul full al piese,
            //vede cu cat se vinde piesa lui in realitate.....

            //atentie la stergere + poate iau si eu functia aia de verifiare daca e piesa furnizorului si o pun in security sau ceva
            app.MapGet("/admin-furnizor/vezi-piese", async (
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                //sa vad daca bag asta, defapt nici nuj, dar daca sunt in /admin-furnizor/xxx. json-ul sa contina furnizor?
                //si aici ceva filtrare, o sa fac toate crudurile si o sa ma fortez sa implementez ceva functie/i care sa nu ma incurce asa de rau
                //tre sa vad si cum fac in _sp.xxxxx_GetAll
                var (eroareFurnizor, furnizorLocalAdmin, idAdminFurnizor) = await SecurityHelper.ObtineFurnizorAdminLocal(admin, connectionString);
                if (eroareFurnizor != null) return eroareFurnizor;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruAdminFurnizor = new DynamicParameters();
                    parametruAdminFurnizor.Add("@idFurnizor", furnizorLocalAdmin.Furnizor_Id);
                    var pieseFurnizori = await connection.QueryAsync<Piese>(
                        "sp_Furnizor_GetPiese",
                        parametruAdminFurnizor,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(new
                    {
                        Piese = pieseFurnizori,
                        Furnizor = furnizorLocalAdmin
                    });
                    
                }
            }).RequireAuthorization();

            app.MapPost("/admin-furnizor/adauga-piesa", async (
                [FromBody] Piese piesaNoua,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                var erori = SecurityHelper.ValideazaDatePiesa(piesaNoua);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });

                var (eroareFurnizor, furnizorLocalAdmin, idAdminFurnizor) = await SecurityHelper.ObtineFurnizorAdminLocal(admin, connectionString);

                if
                using (var connection = new SqlConnection(connectionString))
                {
                    var parametriiPiesaFurnizor = new DynamicParameters();

                    parametriiPiesaFurnizor.Add("@furnizorId", furnizorLocalAdmin.Furnizor_Id);
                    parametriiPiesaFurnizor.Add("@pretCumparare", piesaNoua.Pret_Cumparare);
                    parametriiPiesaFurnizor.Add("@numePiesa", piesaNoua.Nume_Piesa);

                    await connection.ExecuteAsync(
                        "sp_Piesa_FurnizorAdaugaPiesa".
                        parametriiPiesaFurnizor,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(new { message = "Piesa a fost adăugată cu succes!" });
                }

            }).RequireAuthorization();

            app.MapGet("/admin-furnizor/edit-piesa/{idPiesa:int}", async(
                int idPiesa,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;


                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroareFurnizor, furnizorLocalAdmin, idAdminFurnizor) = await SecurityHelper.ObtineFurnizorAdminLocal(admin, connectionString);
                if (eroareFurnizor != null) return eroareFurnizor;

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idPiesa", idPiesa);

                    var piesaDB = await connection.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_FurnizorGetPiesaById",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (piesaDB == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    if (piesaDB.Furnizor_Id != furnizorLocalAdmin.Furnizor_Id)
                    {
                        return Results.BadRequest(new { message = "Piesa care se vrea editata apartine altui furnizor" });
                    }

                    return Results.Ok(new
                    {
                        Piesa = piesaDB,
                    });
                }

            }).RequireAuthorization();

            app.MapPut("/admin-furnizor/edit-piesa/{idPiesa:int}", async (
                [FromBody] Piese piesaEditata,
                int idPiesa, 
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroareFurnizor, furnizorLocalAdmin, idAdminFurnizor) = await SecurityHelper.ObtineFurnizorAdminLocal(admin, connectionString);
                if (eroareFurnizor != null) return eroareFurnizor;

                var erori = SecurityHelper.ValideazaDatePiesa(piesaEditata);

                if (erori.Count > 0)
                    return Results.BadRequest(new { eroriCampuri = erori });


                using (var connection = new SqlConnection(connectionString))
                {

                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idPiesa", idPiesa);

                    var piesaDB = await connection.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_FurnizorGetPiesaById",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (piesaDB == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    if (piesaDB.Furnizor_Id != furnizorLocalAdmin.Furnizor_Id)
                    {
                        return Results.BadRequest(new { message = "Piesa care se vrea editata apartine altui furnizor" });
                    }

                    paramEditarePiesa.Add("@NumePiesa", piesaEditata.Nume_Piesa);
                    paramEditarePiesa.Add("@PretCumparare", piesaEditata.Pret_Cumparare);
                    paramEditarePiesa.Add("@idPiesa", idPiesa);

                    await connection.ExecuteAsync(
                        "sp_Piesa_FurnizorEditPiesa",
                        paramEditarePiesa,
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new { message = "Piesa a fost editata cu succes!" });
                }
                }).RequireAuthorization();

            //si aici tre sa am grija pe viitor sa modific precodura stocata cu cat avansez
            app.MapDelete("/admin-furnizor/delete-piesa/{idPiesa:int}", async(
                int idPiesa,
                ClaimsPrincipal admin,
                IConfiguration config) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroareFurnizor, furnizorLocalAdmin, idAdminFurnizor) = await SecurityHelper.ObtineFurnizorAdminLocal(admin, connectionString);

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idPiesa", idPiesa);

                    var piesaDB = await connection.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_FurnizorGetPiesaById",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (piesaDB == null)
                    {
                        return Results.BadRequest(new { message = "ID inexistent! Cerere proasta!" });
                    }

                    if (piesaDB.Furnizor_Id != furnizorLocalAdmin.Furnizor_Id)
                    {
                        return Results.BadRequest(new { message = "Piesa care se vrea editata apartine altui furnizor" });
                    }

                    int randuriModificate = await connection.QueryFirstOrDefaultAsync<int>(
                        "sp_Piesa_DeletePiesa",
                        parametrii,
                        commandType: CommandType.StoredProcedure);

                    if (randuriModificate == 0)
                    {

                        SecurityHelper.AdaugaEroare(erori, "mesajEroare", "Piesa nu a putut fi stearsa! Ori nu exista, ori nu e a ta!");
                        return Results.BadRequest(new { eroriIdentificator = erori });

                    }

                    return Results.Ok(new { message = "Piesa a fost stearsa cu succes!" });
                }
        }).RequireAuthorization();
        }
    }
}