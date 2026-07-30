using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.DBClasses;
using Backend.Helpers;

namespace Backend.Endpoints
{
    public static class AdminGestioneazaComenziEndpoint
    {
        //tre sa fac si ceva de filtrare etc!
        public static void MapAdminGestioneazaComenziEndpoint(this IEndpointRouteBuilder app)
        {
            //sa vad de aici facturi_furnizor + facturi_furnizor_linie
            //sa vad daca le am primit
            //la iesire, sa vad daca le am prelucrate, si dupa sa le trimit odata cu factura
            //pentru comanda
            //si dupa etc
            //dar le trimitem toate odata aici la comanda
            //trebuie sa iasa cum trebuie
            //odata ce fac endpointu asta, e scurt pe doi si gata
            //si fereasca sfantu sa intru in stergeri.

            app.MapGet("/admin/vezi-logistica-intrare", async (ClaimsPrincipal admin, 
                IConfiguration config
                )=>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {
                    //trebuie sa vad ce fac aici, problema e ca trebuie sa ma gandesc,
                    //dar acuma sunt cam confuz, trebuie sa ma gandesc cum trebuie, pana la virgula
                    //nu e nici prea bine procedura asta, trebuie sa vad eea ce fac cu stadiu_intern, 
                    //ca rupe performanta, of of of.

                    var facturiFurnizori = await connection.QueryAsync<
                        FacturiFurnizor,
                        Furnizor,
                        StatisticiFactura,
                        ComandaPiese,
                        (FacturiFurnizor Factura, Furnizor Furnizor, StatisticiFactura Logistica, ComandaPiese comandaPiese) > (
                        "sp_Furnizor_AdminGeneralGetFacturiIntrare",
                        (factura, furnizor, logistica, comandaPiese) => (factura, furnizor, logistica, comandaPiese),
                        splitOn: "furnizor_id, stadiu_logistica_factura, comanda_piese_id",
                        commandType: CommandType.StoredProcedure
                    );
                    //poate le unific si eu intr-un si aia e
                    //deci aici am toate datele pentru facturile,
                    //adica comenzile care imi vin in fabrica
                    //doamne dumnezeule chiar e nenorocire acili sa
                }
            }).RequireAuthorization();

            //app.MapPost("/admin/receptie-primire/{idFactura:int}")

            app.MapGet("/admin/vezi-logistica-iesire", async (ClaimsPrincipal admin,
                IConfiguration config
                ) =>
            {
                var eroareAutentificare = await SecurityHelper.VerificaAdminGeneral(admin, config);
                var connectionString = config.GetConnectionString("DefaultConnection");

                if (eroareAutentificare != null) return eroareAutentificare;

                using (var connection = new SqlConnection(connectionString))
                {
                    //aici trebuie sa am tot ce iese de aici,
                    //si sa ma asigur ca am ce se vrea trimis
                    //si sa am si aici ceva statistica
                    //asta o sa fie iarasi un endpoint la care trebuie lucrat destul de multicel..
                    //trebuie sa vad ce date vreau sa vad si eu aici
                    //cri cri cri... toamna gri
                    //of complicat
                    var comenziIesire = await connection.QueryAsync<
                        Comanda,
                        Companie,
                        (Comanda Comanda, Companie Companie)>(
                        "sp_Companie_AdminGeneralGetFacturiIesire",
                        (comanda, companie) => (comanda, companie),
                        splitOn: "companie_id",
                        commandType: CommandType.StoredProcedure
                    );
                }
            }).RequireAuthorization();

            //app.MapPost("/admin/trimite-comanda/{idComanda:int}")
        }
    }
}