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

                //etcule, vezi ca trebuie sa uniformizez cumva, si la dashboard, sa arunc functia asta de mi ia utilizator, companie, rol, in security ca e f voluminoasa
                //momenta doar aici, puncte puncte puncte

                using (var connection = new SqlConnection(connectionString))
                {

                    //deci bau bau bau bau bau, am compania, am tot, ma doare capu rau
                    //inca o data, ne trebuie ceva gen dashboard plm
                    //si pedefeul........
                    //tine tot de post
                    //poate si mail trimis la companie + admin?
                }

            }).RequireAuthorization();

            //ma chinui la get, hai sa vezi la post...
        }
    }
}
