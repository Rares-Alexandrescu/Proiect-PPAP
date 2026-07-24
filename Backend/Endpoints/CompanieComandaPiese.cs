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
                    //trebuie sa scot comenzile, cu toate datele, poate intr-o clasa 
                    //ok, cu id uri si tot felu ca sa fie atat modlara, cat si sa nufie 
                    //prea full, of ce greu e

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
                    //o sa fac jsonul fix pentru asta, nu dau toate datele in responsebody
                    //doar pathul de pdf, etcuri de genul asta, id-urile pentru alte chestii care o sa fie aici, aici fac doar comenzile si restul le arunc in alte
                    //endpointuri
                    //am facut clasele exact ca si in db, hai sa fac un json care safie ce e in dashboard + comenzile astea

                    //dar e gata in principiu aici
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
                //o mie de joinuri, sa vad un posibil total si asa mai departe
                //dar trebuie sa vad neaparat toate alea, sa vad cum fac jeisonu
                //dar fac asta inainte de toate, de aici o sa pot sa comand si tot felu de...
                //vreau sa vad fiecare piesa, furnizorul, comentariile, pretul care s a adunat pana acuma pe comanda asta

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
                        splitOn: "cantitate_comandata, nume_furnizor, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);


                    decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                    return Results.Ok(new
                    {
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

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, (Piese Piesa, ComandaPiese Linie, Furnizor Furnizor)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizor) => (piesa, linie, furnizor),
                        parametri,
                        splitOn: "cantitate_comandata, nume_furnizor, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    var linieComanda = rezultate.FirstOrDefault();

                    if (linieComanda.Linie == null)
                        return Results.BadRequest(new { message = "Linia din comanda nu a fost gasita sau nu apartine companiei!" });

                    return Results.Ok(new
                    {
                        Piesa = linieComanda.Piesa,
                        Furnizor = linieComanda.Furnizor,
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

                    //tre sa stearga si sa fie al companiei, si etc, si sa fie si al comenzii

                    //in caz ca se sterg toate elementele,
                    //sa vad daca o sterg sau nu, cred o sterg
                    //sa vad cum o fac.
                    //hai ca o sa vad cum fac, chiar e complicat.
                    //am facut sa se stearga si comanda

                    var parametriiComandaPiesa = new DynamicParameters();
                    parametriiComandaPiesa.Add("@idComandaPiese", idComandaPiesa);
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

                    return Results.Ok(new { message = "Comanda cu id-ul " + idComanda + " a fost modificata cu succes! " + "Linia cu ID-ul " + idComandaPiesa + " a fost stearsa cu succes!" });
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
                        "sp_Companie_GetFurnizoriCuPieseActive",
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new
                    {
                        Furnizori = furnizori
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

            //doamne 1001 probleme, poate maine pe douazeci si unu iulie douamiidouazecisisase fac si pdf si 
            //anghiuleru.... ar fi un vis frumos sp_Piesa_Companie_GetPiesaActivaByPiesaId
            //deci sa vad daca fac logica asta cu facturile, sa vd daca diferentiez comenzile una fata de alta
            //sa pot adauga doar si doar daca e pending
            //ar fi defapt un status, daca e unu, e plasata. daca e zero inca asteapta
            //dar nu am voie sa adaug daca e unu, alta mancare de peste....
            //daca e fac sa vad comenzile, dar nuj daca asta e solutia, sa iau comenzile la care pot, si daca nu are atunci ultima teapa

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
                    //deci aici trebuie sa :
                    //sa adaug si in comanda
                    //sa adaug si in comanda piese
                    //dar si in documente, dar stadiu acceptare ar fi null inca
                    //INCA NU CREEZ FACTURI, DOCUMENTE SI ALTE ETCURI DE GENU
                    //SA VERIFIC DACA COMANDA CERUTA MERGE SA FIE UPDATATA, ALTFEL PULA

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
                    //aici imi trebuie pdf urile, factura generata, si chiar si comenzile penru furnizori.

                    //dar defapt poate mai fac un pas ca sa confirm ca dupa sa se comande si la furnizori
                    //hai ca vedem

                    //si defapt sa verific si daca e deja o comanda plasata
                    //acuma fac PDFService, sa vedem daca il fac bine, dupa ma intorc aici
                    //o sa fie circ

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
                        splitOn: "cantitate_comandata, nume_furnizor, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    if (rezultate == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Comanda e goala!"
                        });
                    }

                    //deci stai ca deja am plesnit-o rau, am documente dar n am factura... uof
                    //functia de pdf mi ar face defapt o factura, sa moara masa
                    //sa zicem ca asta ar fi quote-ul cum ar veni
                    //comanda e plasata, dar tre sa vad oe ce ma bazez
                    //hm, ar mai fi nevoie de o confirmare din partea adminului, doamne ce complicat....
                    //deci comanda --> se face document_compnaie, factura chix. , sa plaseze adminul comanda si dupa sa accepte iar si pentru factura?
                    //sau ar fi o chestie la modul trimisa iar, mai succint, direct dupa ce s a trimis

                    decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                    string calePdfDocumenteComanda = await pdfService.GenereazaPdfComandaAsync(
                        comandaCeruta.comanda_id,
                        companie.Nume_Companie,
                        totalGeneralComanda,
                        rezultate.Cast<dynamic>());

                    //acuma imi trebuie o metoda sa updatez fix asta,am id comanda, trebuie un join
                    //si sa updatez bazat pe asta, sa verific daca este al companiei.... etc
                    //am verificat, am creeat, am pus, daca nu e adevarat sa fac ceva de stergere, chiar daca nu e cea mai buna idee,
                    //doar ma scapa de un sp in plus expres pentru calePdfDocumenteComanda
                    //sp_Documente_Comanda_PlaseazaComanda
                    //taca paca paca, chiar daca am reparat la el, dar vs -ul imi joaca feste

                    //poate fac facturile de furnizor aici, doi in unu
                    //daca nu, o sa vad eu cum fac, cel mai probabil o sa pun ceva serviciu sa mi verifice din ora n ora si aia e
                    //o sa vad, macar acuma si a revenit, bine ca am avut o premonitie

                    parametruComanda.Add("@path_documente_pdf", calePdfDocumenteComanda);

                    await connection.ExecuteAsync(
                        "sp_Documente_Comanda_PlaseazaComanda",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(new { message = "Comanda a fost plasata cu succes!", CalePdf = calePdfDocumenteComanda });

                }
            }).RequireAuthorization();
        }
    }
        //aici definesc clasele noi pentru asta etc
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
