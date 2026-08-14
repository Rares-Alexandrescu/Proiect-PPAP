
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Endpoints
{
    public static class CompanieComandaPieseEndpoint
    {
        public static void MapCompanieComandaPieseEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/compania-ta/comenzi-curente", async (
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametriiCompanie = new DynamicParameters();
                    parametriiCompanie.Add("@idCompanie", companie!.Companie_Id);

                    var comenziCompanie = await connection.QueryAsync<Comanda>(
                        "sp_Companie_GetComenziByCompanieId",
                        parametriiCompanie,
                        commandType: CommandType.StoredProcedure
                        );
                    //poate mai ac ceva aici sa vad statusul si tot felu de chestii

                    var documenteComandaCompanie = await connection.QueryAsync<DocumenteComanda>(
                        "sp_Companie_GetDocumenteComandaByCompanieId",
                        parametriiCompanie,
                        commandType: CommandType.StoredProcedure
                        );

                    var facturaCompanieComanda = await connection.QueryAsync<FacturaCompanie>(
                        "sp_Companie_GetFacturaCompanieByCompanieId",
                        parametriiCompanie,
                        commandType: CommandType.StoredProcedure
                        );

                    var comenziComplete = comenziCompanie.Select(comanda =>
                    {
                        var documentAferent = documenteComandaCompanie
                            .FirstOrDefault(doc => doc.documente_id == comanda.documente_id);

                        var facturaAferenta = facturaCompanieComanda
                            .FirstOrDefault(f => f.comanda_id == comanda.comanda_id);

                        return new
                        {
                            Comanda = comanda,
                            DocumentComanda = documentAferent,
                            Factura = facturaAferenta
                        };
                    }).ToList();

                    return Results.Ok(new
                    {
                        Utilizator = new
                        {
                            utilizator.Id,
                            utilizator.Email,
                            utilizator.Nume,
                            utilizator.Prenume
                        },
                        Rol = rol != null ? rol.ToString() : "N/A",
                        Companie = companie,
                        Comenzi = comenziComplete
                    });
                    //si mai trebuie, macar sa fie asa baza din dashboard
                }
            }).RequireAuthorization();

            app.MapGet("/compania-ta/vezi-comanda/{idComanda:int}", async (
                int idComanda,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    //join cu comanda_piese, dupa cu piese si dupa cu furnizor, dar sa verific si daca are voie sa umble in ea
                    //si sa fac si pdf-ul sa se retina in /Backend cu un path dat din config

                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!,
                        false);

                    if (eroareComanda != null) return eroareComanda;

                    //sp_Comanda_GetPieseDetaliateCompanie
                    //de unde scot piesele comandate sub forma Piese, Comanda_Piese, si suma la toata factura
                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", idComanda);
                    parametruComanda.Add("@idCompanie", companie.Companie_Id);

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                        parametruComanda,
                        splitOn: "comanda_piese_id, furnizor_id, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    if (rezultate == null || !rezultate.Any())
                    {
                        return Results.BadRequest(new { message = "Comanda e goala sau nu exista!" });
                    }

                    decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                    return Results.Ok(new
                    {
                        RolUtilizator = rol,
                        Comanda = comandaCeruta,
                        TotalGeneral = totalGeneralComanda,
                        PieseComandate = rezultate.Select(item => new
                        {
                            Piesa = item.Piesa,
                            FurnizorPiesa = item.furnizorPiesa,
                            DetaliiComandaPiesa = item.Linie,
                            PretTotalRand = item.PretPiese
                        })
                    });

                }

            }).RequireAuthorization();


            app.MapGet("/compania-ta/modifica-comanda/{idComanda:int}/{idComandaPiesa:int}", async (
                int idComanda,
                int idComandaPiesa,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareComanda != null) return eroareComanda;


                    var parametri = new DynamicParameters();
                    parametri.Add("@idComanda", idComanda);
                    parametri.Add("@idCompanie", companie.Companie_Id);
                    parametri.Add("@idComandaPiesa", idComandaPiesa);

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                        parametri,
                        splitOn: "comanda_piese_id, furnizor_id, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    var linieComanda = rezultate.FirstOrDefault();

                    if (linieComanda.Linie == default)
                        return Results.BadRequest(new { message = "Linia din comanda nu a fost gasita sau nu apartine companiei!" });

                    return Results.Ok(new
                    {
                        Piesa = linieComanda.Piesa,
                        Furnizor = linieComanda.furnizorPiesa,
                        PretUnitar = linieComanda.PretPiese,
                        DetaliiComandaPiesa = linieComanda.Linie
                    });
                }
            }).RequireAuthorization();

            app.MapPut("/compania-ta/modifica-comanda/{idComanda:int}/{idComandaPiesa:int}", async (
                [FromBody] AdaugaPiesaRequest editDetaliiPiesa,
                int idComanda,
                int idComandaPiesa,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                //SecurityHelper.ValideazaDateAdaugaPiesa(...)

                //sa vad daca fac si ceva gen sa schimb si piesa, dar smr masa complicat daca ar fi sa
                //schimbe si piesa... hai ca o las asa... momentan macar

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareComanda != null) return eroareComanda;

                    var eroriValidare = SecurityHelper.ValideazaDateAdaugaPiesa(editDetaliiPiesa);

                    if (eroriValidare.Any())
                    {
                        return Results.BadRequest(new { eroriCampuri = eroriValidare });
                    }

                    var parametriiComandaPiesa = new DynamicParameters();
                    parametriiComandaPiesa.Add("@idComandaPiese", idComandaPiesa);
                    parametriiComandaPiesa.Add("@idComanda", idComanda);
                    parametriiComandaPiesa.Add("@idCompanie", companie.Companie_Id);
                    parametriiComandaPiesa.Add("@detaliiPiese", editDetaliiPiesa.DetaliiPiese);
                    parametriiComandaPiesa.Add("@cantitatePiesa", editDetaliiPiesa.Cantitate);

                    int statusUpdate = await connection.ExecuteScalarAsync<int>(
                        "sp_ComandaPiese_CompanieModificaByComandaPieseID",
                        parametriiComandaPiesa,
                        commandType: CommandType.StoredProcedure);

                    if (statusUpdate <= 0)
                        return Results.BadRequest(new { message = "Nu s-a updatat comanda!" });

                    return Results.Ok(new { message = "Comanda cu id-ul " + idComanda + " a fost updatata cu succes!" });
                }


            }).RequireAuthorization();

            app.MapDelete("/compania-ta/sterge-din-comanda/{idComanda:int}/{idComandaPiese:int}", async (
                int idComanda,
                int idComandaPiese,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareComanda != null) return eroareComanda;

                    //tre sa stearga si sa fie al companiei, si etc, si sa fie si al comenzii

                    //in caz ca se sterg toate elementele,
                    //sa vad daca o sterg sau nu, cred o sterg
                    //sa vad cum o fac.
                    //hai ca o sa vad cum fac, chiar e complicat.
                    //am facut sa se stearga si comanda

                    var parametriiComandaPiesa = new DynamicParameters();
                    parametriiComandaPiesa.Add("@idComandaPiese", idComandaPiese);
                    parametriiComandaPiesa.Add("@idComanda", idComanda);
                    parametriiComandaPiesa.Add("@idCompanie", companie.Companie_Id);

                    int statusDelete = await connection.ExecuteScalarAsync<int>(
                        "sp_ComandaPiese_CompanieStergeByComandaPieseID",
                        parametriiComandaPiesa,
                        commandType: CommandType.StoredProcedure);

                    if (statusDelete <= 0)
                        return Results.BadRequest(new { message = "Nu s-a sters comanda!" });

                    if (statusDelete > 1)
                        return Results.Ok(new { message = "Comanda a ramas goala, s-a sters toata comanda cu id-ul " + idComanda });

                    return Results.Ok(new { message = "Comanda cu id-ul " + idComanda + " a fost modificata cu succes! " + "Linia cu ID-ul " + idComandaPiese + " a fost stearsa cu succes!" });
                }

            }).RequireAuthorization();

            app.MapDelete("/compania-ta/sterge-comanda/{idComanda:int}", async (
                int idComanda,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareComanda != null) return eroareComanda;

                    //aici tre sa vad cum sterg toata comanda
                    //sp_Comanda_Companie_StergeComanda
                    //daca e comanda, plm, se verifica statusu in security...
                    //doamne o mie de linii de endpoint, nici n am facut frotnendu si conexiunile, si e joi
                    //mai e mult pana departe

                    //daca e plasata, sterg inainte din comanda_piese, dupa din documente si dupa aceea sterg tot 

                    var parametriiComanda = new DynamicParameters();
                    parametriiComanda.Add("@idComanda", idComanda);
                    parametriiComanda.Add("@idCompanie", companie.Companie_Id);

                    int statusDelete = await connection.ExecuteScalarAsync<int>(
                        "sp_Comanda_Companie_StergeComanda",
                        parametriiComanda,
                        commandType: CommandType.StoredProcedure);

                    if (statusDelete <= 0)
                        return Results.BadRequest(new { message = "Nu s-a sters comanda!" });

                    return Results.Ok(new { message = "Comanda cu id-ul " + comandaCeruta.comanda_id + " s-a sters cu succes !" });
                }

            }).RequireAuthorization();

            app.MapGet("/compania-ta/noua-comanda", async (
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var furnizori = await connection.QueryAsync<FurnizorCuPieseActive>(
                        "sp_Furnizor_GetFurnizoriCuPieseActive",
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new
                    {
                        Furnizori = furnizori.Select(f => new
                        {
                            f.NumarPieseActive,
                            f.Furnizor_Id,
                            f.Numar_Telefon,
                            f.Email_Furnizor,
                            f.Nume_Furnizor,
                            f.CNP_Admin_Furnizor
                        })
                    });
                }

            }).RequireAuthorization();

            app.MapGet("/compania-ta/noua-comanda/{idFurnizor:int}", async (
                int idFurnizor,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametrii = new DynamicParameters();
                    parametrii.Add("@idFurnizor", idFurnizor);

                    var piese = await connection.QueryAsync<Piese>(
                        "sp_Piesa_Companie_GetPieseActiveByFurnizorId",
                        parametrii,
                        commandType: CommandType.StoredProcedure
                    );

                    if (piese == null)
                        return Results.BadRequest(new { message = "Bau bau bau Nu trebuie sa apara asta etc" });

                    return Results.Ok(piese);
                }
            }).RequireAuthorization();

            app.MapGet("/compania-ta/adauga-piesa/{idFurnizor:int}/{idPiesa:int}", async (
                int idFurnizor,
                int idPiesa,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruPiesa = new DynamicParameters();
                    parametruPiesa.Add("@idPiesa", idPiesa); //get piesa doar pentru companie,nu ne trebuie date inutile

                    var piesaCompanie = await connection.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_Companie_GetPiesaActivaByPiesaId",
                        parametruPiesa,
                        commandType: CommandType.StoredProcedure
                    );

                    if (piesaCompanie == null || piesaCompanie.Furnizor_Id != idFurnizor)
                        return Results.BadRequest(new { message = "Bau bau bau Nu trebuie sa apara asta etc" });

                    return Results.Ok(piesaCompanie);
                }

            }).RequireAuthorization();


            app.MapPost("/compania-ta/adauga-piesa/{idFurnizor:int}/{idPiesa:int}", async (
                    int idFurnizor,
                    int idPiesa,
                    [FromBody] AdaugaPiesaRequest adaugaPiesa,
                    ClaimsPrincipal utilizatorCompanie,
                    IConfiguration config) =>
            {
                //tre sa verific id-ul furnizor, direct din backend, bazat pe idPiesa. 
                //etc
                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametruPiesa = new DynamicParameters();
                    parametruPiesa.Add("@idPiesa", idPiesa); //get piesa doar pentru companie,nu ne trebuie date inutile

                    var piesaCompanie = await connection.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_Companie_GetPiesaActivaByPiesaId",
                        parametruPiesa,
                        commandType: CommandType.StoredProcedure
                    );

                    if (piesaCompanie == null || piesaCompanie.Furnizor_Id != idFurnizor)
                        return Results.BadRequest(new { message = "Bau bau bau Nu trebuie sa apara asta etc" });


                    var eroriValidare = SecurityHelper.ValideazaDateAdaugaPiesa(adaugaPiesa);

                    if (eroriValidare.Any())
                    {
                        return Results.BadRequest(new { eroriCampuri = eroriValidare });
                    }

                    if (!adaugaPiesa.Comanda_Id.HasValue || adaugaPiesa.Comanda_Id.Value <= 0)
                    {
                        //aici cream documente_comanda....
                        var parametruCompanie = new DynamicParameters();
                        parametruCompanie.Add("@idCompanie", companie.Companie_Id);

                        int idDocumenteComanda = await connection.ExecuteScalarAsync<int>(
                            "sp_Documente_Comanda_IncepeComanda",
                            parametruCompanie,
                            commandType: CommandType.StoredProcedure);

                        //aici cream comanda si tot felu de...

                        var parametruDocumentCompanie = new DynamicParameters();
                        parametruDocumentCompanie.Add("@idDocumenteComanda", idDocumenteComanda);

                        int idComanda = await connection.ExecuteScalarAsync<int>(
                            "sp_Comanda_IncepeComanda",
                            parametruDocumentCompanie,
                            commandType: CommandType.StoredProcedure);
                        //dupa facem comanda_piese

                        var parametriiComandaPiese = new DynamicParameters();
                        parametriiComandaPiese.Add("@idComanda", idComanda);
                        parametriiComandaPiese.Add("@idPiesa", idPiesa);
                        parametriiComandaPiese.Add("@cantitateComandata", adaugaPiesa.Cantitate);
                        parametriiComandaPiese.Add("@detalii_piese", string.IsNullOrWhiteSpace(adaugaPiesa.DetaliiPiese) ? null : adaugaPiesa.DetaliiPiese);

                        int statusAdaugare = await connection.ExecuteScalarAsync<int>(
                            "sp_Comanda_Piesa_AdaugaPiesa",
                            parametriiComandaPiese,
                            commandType: CommandType.StoredProcedure);

                        if (statusAdaugare > 0)
                            return Results.Ok(new { message = "Piesa " + piesaCompanie.Nume_Piesa + " a/au fost adaugate cu succes!" });
                        else
                            return Results.BadRequest(new { message = "Eroare! Nu am putut sa adaugam piesa!" });
                        //dupa documente_comanda, cu date fasaite pana dam post-ul de comanda efectiv
                    }
                    else
                    {

                        var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                            adaugaPiesa.Comanda_Id.Value,
                            companie.Companie_Id,
                            connectionString!);

                        if (eroareComanda != null) return eroareComanda;

                        //adaugam in comanda_piese
                        var parametriiComandaPiese = new DynamicParameters();
                        parametriiComandaPiese.Add("@idComanda", comandaCeruta.comanda_id);
                        parametriiComandaPiese.Add("@idPiesa", idPiesa);
                        parametriiComandaPiese.Add("@cantitateComandata", adaugaPiesa.Cantitate);
                        parametriiComandaPiese.Add("@detalii_piese", adaugaPiesa.DetaliiPiese);

                        int statusAdaugare = await connection.ExecuteScalarAsync<int>(
                            "sp_Comanda_Piesa_AdaugaPiesa",
                            parametriiComandaPiese,
                            commandType: CommandType.StoredProcedure);

                        if (statusAdaugare > 0)
                            return Results.Ok(new
                            {
                                message = "Piesa " + piesaCompanie.Nume_Piesa + " a/au fost adaugate cu succes!",
                                idComanda = comandaCeruta.comanda_id
                            });
                        else
                            return Results.BadRequest(new { message = "Eroare! Nu am putut sa adaugam piesa!" });

                        //si lasa in pace in documente_comanda
                    }
                }

            }).RequireAuthorization();

            app.MapPost("/compania-ta/plaseaza-comanda/{idComanda:int}", async (
                int idComanda,
                ClaimsPrincipal utilizatorCompanie,
                IPDFService pdfService,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(utilizatorCompanie, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici, desi a verificat adminlocal....!" });


                using (var connection = new SqlConnection(connectionString))
                {
                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareComanda != null) return eroareComanda;

                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", idComanda);
                    parametruComanda.Add("@idCompanie", companie.Companie_Id);

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                        parametruComanda,
                        splitOn: "comanda_piese_id, furnizor_id, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    if (rezultate == null || !rezultate.Any())
                    {
                        return Results.BadRequest(new
                        {
                            message = "Comanda e goala!"
                        });
                    }


                    decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                    var pieseFormatatePentruPdf = rezultate.Select(r => new
                    {
                        Piesa = r.Piesa,
                        FurnizorPiesa = r.furnizorPiesa,
                        DetaliiComandaPiesa = r.Linie,
                        PretPiese = r.PretPiese
                    }).ToList();

                    string calePdfDocumenteComanda = await pdfService.GenereazaPdfComandaAsync(
                        comandaCeruta.comanda_id,
                        companie.Nume_Companie,
                        totalGeneralComanda,
                        pieseFormatatePentruPdf);

                    parametruComanda.Add("@path_documente_pdf", calePdfDocumenteComanda);

                    await connection.ExecuteAsync(
                        "sp_Documente_Comanda_PlaseazaComanda",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(new { message = "Comanda a fost plasata cu succes!", CalePdf = calePdfDocumenteComanda });

                }
            }).RequireAuthorization();

            app.MapPut("/compania-ta/receptioneaza-comanda/{idComanda:int}", async (
                int idComanda,
                ClaimsPrincipal adminCompanie,
                IPDFService pdfService,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(adminCompanie, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(adminCompanie, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici, desi a verificat adminlocal....!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var (eroareComanda, comandaCeruta) = await SecurityHelper.VerificaSiObtineComandaDupaId(
                        idComanda,
                        companie.Companie_Id,
                        connectionString!,
                        false);

                    if (eroareComanda != null) return eroareComanda;

                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", idComanda);

                    int rezultatCommanda = await connection.ExecuteAsync("sp_Documente_Comanda_CompaniaReceptioneazaComanda",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);

                    if (rezultatCommanda == 0)
                    {
                        return Results.BadRequest(new { message = "Nu s-a putut receptiona comanda!" });
                    }

                    return Results.Ok(new { message = "S-a receptionat comanda cu succes!" });

                }
            }).RequireAuthorization();

            app.MapPut("/compania-ta/plateste-factura/{idFactura:int}", async(int idFactura, 
                ClaimsPrincipal admin, 
                IConfiguration config, 
                IEmailService emailService) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminLocal(admin, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(admin, connectionString!);
                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici, desi a verificat adminlocal....!" });

                using (var connection = new SqlConnection(connectionString))
                {

                    var (eroareFactura, facturaCeruta) = await SecurityHelper.VerificaSiObtineFacturaCompanieDupaId(
                        idFactura,
                        companie.Companie_Id,
                        connectionString!);

                    if (eroareFactura != null) return eroareFactura;

                    var parametruFactura = new DynamicParameters();
                    parametruFactura.Add("@idFactura", idFactura);

                    int rezultatFactura = await connection.ExecuteAsync("sp_Factura_Companie_PlatesteFactura",
                        parametruFactura,
                        commandType: CommandType.StoredProcedure);

                    if (rezultatFactura == 0)
                        return Results.BadRequest(new { message = "Nu s-a platit factura!" });

                    var dateEmail = await connection.QueryAsync(
                        "sp_Furnizor_AdminGetFacturaPentruEmail",
                        parametruFactura,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(new { message = "Factura platita cu succes!" });

                }
            }).RequireAuthorization();

            app.MapGet("/compania-ta/download-factura/{idFactura:int}", async (
                int idFactura,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametri = new DynamicParameters();
                    parametri.Add("@idFactura", idFactura);
                    parametri.Add("@idCompanie", companie.Companie_Id);

                    var calePdf = await connection.ExecuteScalarAsync<string>(
                        "sp_Companie_GetFacturaCompaniePathPdf",
                        parametri,
                        commandType: CommandType.StoredProcedure);

                    if (string.IsNullOrEmpty(calePdf))
                    {
                        return Results.BadRequest(new { message = "Factura nu exista sau nu apartine companiei dumneavoastra." });
                    }

                    var caleFizica = Path.Combine(Directory.GetCurrentDirectory(), calePdf.TrimStart('/'));

                    if (!File.Exists(caleFizica))
                    {
                        return Results.BadRequest(new { message = "Fisierul nu a fost gasit pe server." });
                    }

                    var bytes = await File.ReadAllBytesAsync(caleFizica);
                    return Results.File(bytes, "application/pdf", $"Factura_{companie.Nume_Companie}_{idFactura}.pdf");
                }
            }).RequireAuthorization();

            app.MapGet("/compania-ta/download-documentatie/{idDocumenteComanda:int}", async (
                int idDocumenteComanda,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                if (companie == null)
                    return Results.BadRequest(new { message = "Nu are companie, nu are voie aici!" });

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametri = new DynamicParameters();
                    parametri.Add("@idDocumenteComanda", idDocumenteComanda);
                    parametri.Add("@idCompanie", companie.Companie_Id);

                    var calePdf = await connection.ExecuteScalarAsync<string>(
                        "sp_Companie_GetDocumenteComandaPathPdf",
                        parametri,
                        commandType: CommandType.StoredProcedure);

                    if (string.IsNullOrEmpty(calePdf))
                    {
                        return Results.BadRequest(new { message = "Documentul nu exista sau nu apartine companiei dumneavoastra." });
                    }

                    var caleFizica = Path.Combine(Directory.GetCurrentDirectory(), calePdf.TrimStart('/'));

                    if (!File.Exists(caleFizica))
                    {
                        return Results.NotFound(new { message = "Fisierul nu a fost gasit pe server." });
                    }

                    var bytes = await File.ReadAllBytesAsync(caleFizica);
                    return Results.File(bytes, "application/pdf", $"Documentatie_{companie.Nume_Companie}_{idDocumenteComanda}.pdf");
                }
            }).RequireAuthorization();
        }
    }
    public class AdaugaPiesaRequest
    {
        public int? Comanda_Id { get; set; }
        public int Cantitate { get; set; } = 1;
        public string? DetaliiPiese { get; set; }
    }

    public class FurnizorCuPieseActive : Backend.DBClasses.Furnizor
    {
        public int NumarPieseActive { get; set; } = 0;
    }
}
