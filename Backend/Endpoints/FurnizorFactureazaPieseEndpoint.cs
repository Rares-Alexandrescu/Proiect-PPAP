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
    public static class FurnizorFactureazaPieseEndpoint
    {
        public static void MapFurnizorFactureazaPieseEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/admin-furnizor/vezi-facturi",  async(
                ClaimsPrincipal adminFurnizor,
                IConfiguration config)=>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if(erori != null) return erori;

                using( var connection = new SqlConnection(connectionString) )
                {
                    var parametruAdminFurnizor = new DynamicParameters();
                    parametruAdminFurnizor.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);

                    var facturi = await connection.QueryAsync<
                        FacturiFurnizor,
                        StatisticiFactura,
                        (FacturiFurnizor Factura, StatisticiFactura Logistica)>(

                        "sp_Furnizor_GetFacturiFurnizorByFurnizorId",
                        (factura, logistica) => (factura, logistica),
                        parametruAdminFurnizor,
                        splitOn: "stadiu_logistica_factura",            
                        commandType: CommandType.StoredProcedure
                    );

                    return Results.Ok(new
                    {
                        ListaFacturi = facturi.Select(item => new
                        {
                            Factura = item.Factura,     
                            StatisticiFactura = item.Logistica   
                        }),
                        Furnizor = furnizorAdmin
                    });
                }


            }).RequireAuthorization();

            //asta nu e gata inca, tre sa vad si piesa si eu vad daor id-ul piesei
            app.MapGet("/admin-furnizor/vezi-factura/{idFactura:int}", async (
                int idFactura,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametruAdminFurnizor = new DynamicParameters();
                    parametruAdminFurnizor.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);
                    parametruAdminFurnizor.Add("@idFactura", idFactura);

                    var facturaDetaliata = await connection.QueryAsync<
                        FacturiFurnizor,
                        FacturiFurnizorLinie,
                        Piese,
                        int,
                        StatisticiFactura,
                        (FacturiFurnizor Factura, FacturiFurnizorLinie detaliiFactura, Piese piesaLinie, int cantitatePiese, StatisticiFactura Logistica)>(
                        "sp_Furnizor_GetFacturaFurnizorByFurnizorId",
                        (factura, linie, piesaLinie, cantitatePiese, logistica) => (factura, linie, piesaLinie, cantitatePiese, logistica),
                        parametruAdminFurnizor,
                        splitOn: "facturi_linie_id, piese_id, cantitate_comandata, stadiu_logistica_factura",
                        commandType: CommandType.StoredProcedure
                    );

                    if(facturaDetaliata == null || !facturaDetaliata.Any())
                    {
                        return Results.BadRequest(new { message = "Nu exista factura, sau nu e factura dumneavoastra!" });
                    }

                    var statistici = facturaDetaliata.First();

                    var facturiLinii = facturaDetaliata
                    .Where(x => x.detaliiFactura != null && x.detaliiFactura.facturi_linie_id > 0)
                    .Select(x => new
                    {
                        DetaliiFactura = x.detaliiFactura,
                        Piesa = x.piesaLinie,
                        Cantitate = x.cantitatePiese
                    })
                    .ToList();

                    return Results.Ok(new
                    {
                        Factura = statistici.Factura,
                        Statistici = statistici.Logistica,
                        Linii = facturiLinii
                    });

                }
            }).RequireAuthorization();


            //la posturile astea doua trebuie sa le trimit numai cand a emis si furnizoru factura
            //bine, momentan doar trec aia pe unu, dar ideea e ca se poate trimite ori de cate ori
            //este o problema? nu stiu.

            app.MapPost("/admin-furnizor/trimite-comanda/{idFactura:int}", async (
                int idFactura,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config) =>

            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

                using (var connection = new SqlConnection(connectionString))
                {
                    var parametriTrimitere = new DynamicParameters();
                    parametriTrimitere.Add("@idFactura", idFactura);
                    parametriTrimitere.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);

                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_Furnizor_TrimiteComandaSauLinie",
                        parametriTrimitere,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                    {
                        return Results.BadRequest(new { message = "Nu exista factura, sau nu apartine furnizorului dumneavoastra!" });
                    }
                    Console.WriteLine("S-au trimis " + randuriAfectate + " linii ca si unu");
                    return Results.Ok(new { message = "Comanda a fost marcata ca trimisa."});
                }

            }).RequireAuthorization();

            app.MapPost("/admin-furnizor/trimite-linia-comanda/{idFactura:int}/{idFacturaLinie:int}", async(
                int idFactura,
                int idFacturaLinie,
                ClaimsPrincipal adminFurnizor,
                IConfiguration config)=>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametriTrimitere = new DynamicParameters();
                    parametriTrimitere.Add("@idFactura", idFactura);
                    parametriTrimitere.Add("@idFacturaLinie", idFacturaLinie);
                    parametriTrimitere.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);

                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_Furnizor_TrimiteComandaSauLinie",
                        parametriTrimitere,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                    {
                        return Results.BadRequest(new { message = "Nu exista factura, sau nu apartine furnizorului dumneavoastra!" });
                    }

                    Console.WriteLine("S-au trimis " + randuriAfectate + " linii ca si unu");
                    return Results.Ok(new { message = "Linia de comanda a fost marcata ca trimisa."});
                }

            }).RequireAuthorization();

            app.MapPost("/admin-furnizor/genereaza-facturi", async(
                ClaimsPrincipal adminFurnizor,
                IPDFService pdfService,
                IConfiguration config) =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                var eroareAutentificare = await SecurityHelper.VerificaAdminFurnizor(adminFurnizor, config);
                if (eroareAutentificare != null) return eroareAutentificare;

                var (erori, furnizorAdmin, idAdminFurnizor) = SecurityHelper.ObtineFurnizorAdminLocal(adminFurnizor, config);
                if (erori != null) return erori;

                using (var connection = new SqlConnection(connectionString))
                {

                    var parametriTrimitere = new DynamicParameters();
                    parametriTrimitere.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);
                    
                    //SI CEVA DE PDF, HAI CA VEDEM CUM FACEM...
                    //fac de aici, o sa trebuiasca sa fac aci
                    //momentan nici nu e facut sp=ul puncte puncte puncte
                    
                    //in pdf imi trebuie datele furnizorului, piese, liniile de factura, factura

                    var idFactura = await connection.ExecuteScalarAsync<int>(
                    "sp_Furnizor_GenereazaFacturi",
                    parametriTrimitere,
                    commandType: CommandType.StoredProcedure);

                    if (idFactura == 0)
                    {
                        return Results.Ok(new { message = "Nu s-au generat facturi." });
                    }

                    parametriTrimitere.Add("@idFactura", idFactura);

                    var rezultate = await connection.QueryAsync<
                        FacturiFurnizor,
                        Piese,
                        TotalPiesaFactura,
                        (FacturiFurnizor Factura, Piese Piesa, TotalPiesaFactura Total) > (
                        "sp_Furnizor_GetFacturaPentruPdf",
                        (factura, piesa, total) => (factura, piesa, total),
                        parametriTrimitere,
                        splitOn: "piese_id, CantitateTotala",
                        commandType: CommandType.StoredProcedure
                    );

                    if (rezultate == null || !rezultate.Any())
                    {
                        return Results.BadRequest(new { message = "Nu exista factura, sau nu apartine furnizorului dumneavoastra!" });
                    }

                    var factura = rezultate.First().Factura;

                    var liniiFactura = rezultate.Select(r => (r.Piesa, r.Total));

                    //doamne nu stiu cum sa scot astea cum trebuie puncte puncte puncte

                    //aucu fac asta cu pdf-ul, mult mai complicat ca la companie uof
                    //tre a fac si un serviu de isntalare direct din brauzer
                    var caleFisier = await pdfService.GenereazaPdfFacturaFurnizorAsync(
                        factura,
                        furnizorAdmin.Nume_Furnizor,
                        liniiFactura
                    );

                    //tre sa o pun in db pe pdf_cale...
                    var parametruFacturaFurnizor = new DynamicParameters();
                    parametruFacturaFurnizor.Add("@idFurnizor", furnizorAdmin.Furnizor_Id);
                    parametruFacturaFurnizor.Add("@idFactura", idFactura);
                    parametruFacturaFurnizor.Add("@calePdfFactura", caleFisier);

                    var randuriAfectate = await connection.ExecuteScalarAsync<int>(
                        "sp_FacturiFurnizor_Put_Cale_Pdf",
                        parametruFacturaFurnizor,
                        commandType: CommandType.StoredProcedure
                    );

                    if (randuriAfectate == 0)
                        return Results.BadRequest(new { message = "Problema la a pune path-ul facturii in baza de date!" });

                    return Results.Ok(new { message = "S-a generat factura cu id-ul" + idFactura });
                }

            }).RequireAuthorization();
        }
        public class StatisticiFactura
        {
            public string stadiu_logistica_factura { get; set; } = "Zero";
            public int linii_expediate { get; set; } = 0;
            public int linii_total { get; set; } = 0;
        }
        public class TotalPiesaFactura
        {
            public int CantitateTotala { get; set; }
            public decimal PretTotalPiesa { get; set; }
        }
    }
}