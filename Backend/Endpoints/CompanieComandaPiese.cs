using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class CompanieComandaPieseEndpoint
    {
        public static void MapCompanieComandaPieseEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/compania-ta/comenzi-curente", async(
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString);
                if (eroare != null) return eroare;

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

            //aici trebuie sa iau getul, sa mi dea pentru fiecare furnizor in parte, sa mi completeze o lista interna sau ceva care va popula comanda cu toate alea,
            //smr fam mea
            //deci am getul asta, care trebuie efectiv sa ma ajute sa iau toti furnizorii, dar o sa ma rupa la timp si chestii de genul,
            //tre sa fac posibila si cacatul ala cu comentariu
            //doamne dumnezeule
            //pfpfppppfpfpfppfpfpfpppfpfpfpfpfpfpfpf

            app.MapGet("/compania-ta/noua-comanda", async(
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString);

                if (eroare != null) return eroare;

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

            app.MapGet("/compania-ta/noua-comanda/{idFurnizor:int}/piese-active", async (
                int idFurnizor,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

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
                int idPiesa,
                ClaimsPrincipal utilizatorCompanie,
                IConfiguration config) =>
            {
                //tre sa verific id-ul furnizor, direct din backend, bazat pe idPiesa. 
                //etc

                var connectionString = config.GetConnectionString("DefaultConnection");
                var (eroare, utilizator, rol, companie) = await SecurityHelper.ObtineContextDinJWT(utilizatorCompanie, connectionString!);

                if (eroare != null) return eroare;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruPiesa = new DynamicParameters();
                    parametruPiesa.Add("@idPiesa", idPiesa); //get piesa doar pentru companie,nu ne trebuie date intile

                    var piesaCompanie = connection await.QueryFirstOrDefaultAsync<Piese>(
                        "sp_Piesa_Companie_GetPiesaActivaByPiesaId",
                        parametruPiesa,
                        commandType: CommandType.StoredProcedure
                    );

                    if (piesaCompanie == null || piesaCompanie.)
                        return Results.BadRequest(new { message = "Bau bau bau Nu trebuie sa apara asta etc" });

                    return Results.Ok(piesaCompanie);
                }

            }).RequireAuthorization();

            //doamne 1001 probleme, poate maine pe douazeci iulie douamiidouazecisicinci fac si pdf si 
            //anghiuleru.... ar fi un vis frumos sp_Piesa_Companie_GetPiesaActivaByPiesaId
            //deci sa vad daca fac logica asta cu facturile, sa vd daca diferentiez comenzile una fata de alta
            //sa pot adauga doar si doar daca e pending
            //ar fi defapt un status, daca e unu, e plasata. daca e zero inca asteapta
            //dar nu am voie sa adaug daca e unu, alta mancare de peste....
            //daca e fac sa vad comenzile, dar nuj daca asta e solutia, sa iau comenzile la care pot, si daca nu are atunci ultima teapa
            //smbgpl

            app.MapPost("/compania-ta/adauga-piesa/{idPiesa:int}", async (
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



            }).RequireAuthorization();
        }
    }

    //aici definesc clasele noi pentru asta etc
    public class AdaugaPiesaRequest
    {
        public int? Comanda_Id { get; set; }
        public int Cantitate { get; set; } = 1;
        public string? Comentariu_Cote { get; set; }
    }

    public class FurnizorCuPieseActive : Backend.DBClasses.Furnizor
    {
        public int NumarPieseActive { get; set; } = 0;
    }
}
