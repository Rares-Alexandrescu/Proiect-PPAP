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

                    //o sa fac jsonul fix pentru asta, nu dau toate datele in responsebody
                    //doar pathul de pdf, etcuri de genul asta, id-urile pentru alte chestii care o sa fie aici, aici fac doar comenzile si restul le arunc in alte
                    //endpointuri
                    //am facut clasele exact ca si in db, hai sa fac un json care safie ce e in dashboard + comenzile astea

                    //dar e gata in principiu aici
                    return Results.Ok(new
                    {
                        Utilizator = utilizator,
                        Rol = rol != null ? rol.ToString() : "N/A",
                        Companie = companie
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

                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", idComanda);
                    parametruComanda.Add("@idCompanie", companie.Companie_Id);

                    //luam comanda, verificam daca exista, daca apartine companiei...
                    //daca se poate adauga piesa in comanda specificata

                    var comandaCeruta = await connection.QueryFirstOrDefaultAsync<Comanda>(
                        "sp_Comanda_Companie_GetComandaById",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);


                    if (comandaCeruta == null)
                        return Results.BadRequest(new { message = "Nu exista comanda ceruta!" });

                    //sp_Comanda_GetPieseDetaliateCompanie
                    //de unde scot piesele comandate sub forma Piese, Comanda_Piese, si suma la toata factura

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                        parametruComanda,
                        splitOn: "cantitate_comandata, nume_furnizor, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

                    //deci aici avem si datele noastre
                    //deci tre sa fac si jeisonu

                    decimal totalGeneralComanda = rezultate.FirstOrDefault().TotalPretComanda;

                    return Results.Ok(new
                    {
                        Comanda = comandaCeruta,
                        TotalGeneral = totalGeneralComanda,
                        PieseComandate = rezultate.Select(item => new
                        {
                            Piesa = item.Piesa,
                            FurnizorPiesa = item.FurnizorPiesa,
                            DetaliiComandaPiesa = item.Linie,
                            PretTotalRand = item.PretPiese
                        })
                    });

                }

            }).RequireAuthorization();

            //aici trebuie sa iau getul, sa mi dea pentru fiecare furnizor in parte, sa mi completeze o lista interna sau ceva care va popula comanda cu toate alea,
            //smr fam mea
            //deci am getul asta, care trebuie efectiv sa ma ajute sa iau toti furnizorii, dar o sa ma rupa la timp si chestii de genul,
            //tre sa fac posibila si cacatul ala cu comentariu
            //doamne dumnezeule
            //pfpfppppfpfpfppfpfpfpppfpfpfpfpfpfpfpf

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

                    //deci bau bau bau bau bau, am compania, am tot, ma doare capu rau
                    //inca o data, ne trebuie ceva gen dashboard plm
                    //si pedefeul........
                    //tine tot de post
                    //poate si mail trimis la companie + admin?

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
            //ma chinui la get, hai sa vezi la post...

            app.MapGet("/compania-ta/adauga-piesa/{idFurnizor:int}/{idPiesa:int}", async (
                int idFurnizor,
                int idPiesa,
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
            //smbgpl

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


                if (adaugaPiesa.Cantitate < 0)
                {
                    //tre s fac aici ceva gen eroare de camp, dar nu cred ca e nevoie???????????
                    //sau nici nu stiu
                    //de verificat 
                    return Results.BadRequest(new { message = "Imi da cantitate negativa, suta la suta un glumet!" });
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
                        parametruCompanie,
                        commandType: CommandType.StoredProcedure);
                    //dupa facem comanda_piese

                    var parametriiComandaPiese = new DynamicParameters();
                    parametriiComandaPiese.Add("@idComanda", idComanda);
                    parametriiComandaPiese.Add("@idPiesa", idPiesa);
                    parametriiComandaPiese.Add("@cantitateComandata", adaugaPiesa.Cantitate);
                    parametriiComandaPiese.Add("@detalii_piese", adaugaPiesa.DetaliiPiese);

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

                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", adaugaPiesa.Comanda_Id);
                    parametruComanda.Add("@idCompanie", companie.Companie_Id);
                    //luam comanda, verificam daca exista, daca apartine companiei...
                    //daca se poate adauga piesa in comanda specificata

                    var comandaCeruta = await connection.QueryFirstOrDefaultAsync<Comanda>(
                        "sp_Comanda_Companie_GetComandaById",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);

                    if (comandaCeruta == null)
                        return Results.BadRequest(new { message = "Nu exista comanda ceruta!" });

                    if (comandaCeruta.stadiu_finalizare == true)
                        return Results.BadRequest(new { message = "Comanda e deja plasata, nu poti sa adaugi o piesa intr-o comanda deja depusa" });
                    //adaugam in comanda_piese
                    var parametriiComandaPiese = new DynamicParameters();
                    parametriiComandaPiese.Add("@idComanda", idComanda);
                    parametriiComandaPiese.Add("@idPiesa", idPiesa);
                    parametriiComandaPiese.Add("@cantitateComandata", adaugaPiesa.Cantitate);
                    parametriiComandaPiese.Add("@detalii_piese", adaugaPiesa.DetaliiPiese);

                    int statusAdaugare = await connection.ExecuteScalarAsync<int>(
                        "sp_Comanda_Piesa_AdaugaPiesa",
                        parametriiComandaPiese,
                        commandType: CommandType.StoredProcedure);

                    if (statusAdaugare > 0)
                        return Results.Ok(new { message = "Piesa " + piesaCompanie.Nume_Piesa + " a/au fost adaugate cu succes!" });
                    else
                        return Results.BadRequest(new { message = "Eroare! Nu am putut sa adaugam piesa!" });

                    //si lasa in pace in documente_comanda
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

                    var parametruComanda = new DynamicParameters();
                    parametruComanda.Add("@idComanda", idComanda);
                    parametruComanda.Add("@idCompanie", companie.Companie_Id);


                    var comandaCeruta = await connection.QueryFirstOrDefaultAsync<Comanda>(
                        "sp_Comanda_Companie_GetComandaById",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);


                    if (comandaCeruta == null)
                        return Results.BadRequest(new { message = "Nu exista comanda ceruta!" });

                    if (comandaCeruta.stadiu_finalizare == true)
                        return Results.BadRequest(new { message = "Comanda a fost deja plasata!" });

                    var rezultate = await connection.QueryAsync<Piese, ComandaPiese, Furnizor, decimal, decimal, (Piese Piesa, ComandaPiese Linie, Furnizor furnizorPiesa, decimal PretPiese, decimal TotalPretComanda)>(
                        "sp_Comanda_GetPieseDetaliateCompanie",
                        (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda) => (piesa, linie, furnizorPiesa, pretPiese, TotalPretComanda),
                        parametruComanda,
                        splitOn: "cantitate_comandata, nume_furnizor, pretPiese, TotalPretComanda",
                        commandType: CommandType.StoredProcedure);

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
                        rezultate);

                    //acuma imi trebuie o metoda sa updatez fix asta,am id comanda, trebuie un join
                    //si sa updatez bazat pe asta, sa verific daca este al companiei.... etc
                    //am verificat, am creeat, am pus, daca nu e adevarat sa fac ceva de stergere, chiar daca nu e cea mai buna idee,
                    //doar ma scapa de un sp in plus expres pentru calePdfDocumenteComanda
                    //sp_Documente_Comanda_PlaseazaComanda
                    //taca paca paca, chiar daca am reparat la el, dar vsc -ul imi joaca feste
                    //poate fac facturile de furnizor aici, doi in unu
                    parametruComanda.Add("@path_documente_pdf", calePdfDocumenteComanda);

                    await connection.ExecuteAsync(
                        "sp_Documente_Comanda_PlaseazaComanda",
                        parametruComanda,
                        commandType: CommandType.StoredProcedure);

                }
            }).RequireAuthorization();
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
}
