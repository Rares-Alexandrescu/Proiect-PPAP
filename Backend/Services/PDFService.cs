using Backend.PDFTemplates;
using QuestPDF.Fluent;
using Backend.Endpoints;
using Backend.DBClasses;

namespace Backend.Services
{
    public interface IPDFService
    {
        Task<string> GenereazaPdfComandaAsync(int idComanda, string numeCompanie, decimal totalGeneral, IEnumerable<dynamic> pieseComandate);
        Task<string> GenereazaPdfFacturaFurnizorAsync(FacturiFurnizor factura, string numeFurnizor, IEnumerable<(Piese Piesa, TotalPiesaFactura Total)> liniiFactura);
    }

    public class PDFService : IPDFService
    {
        private readonly IConfiguration _config;

        public PDFService(IConfiguration configuration)
        {
            _config = configuration;
        }

        public async Task<string> GenereazaPdfComandaAsync(int idComanda, string numeCompanie, decimal totalGeneral, IEnumerable<dynamic> pieseComandate)
        {
            //sa vedem aici la folderabsolut, sa nu fie o problema in viitor
            string folderRelativ = _config["PDFSettings:PathFolderSalvarePDF"] ?? "PdfComenziSalvate";
            string folderAbsolut = Path.Combine(Directory.GetCurrentDirectory(), folderRelativ);

            if (!Directory.Exists(folderAbsolut))
            {
                Console.WriteLine("ATENTIE CA S A CREAT, INSEAMNA CA NU EXISTA");
                Directory.CreateDirectory(folderAbsolut);
            }

            var pdfDocument = new ComandaPDFDocument(idComanda, numeCompanie, totalGeneral, pieseComandate);
            byte[] pdfBytes = pdfDocument.GeneratePdf();

            string numeFisier = $"Comanda_{idComanda}_{numeCompanie}_{Guid.NewGuid().ToString()[..8]}.pdf";
            string caleSalvareCompleta = Path.Combine(folderAbsolut, numeFisier);

            await File.WriteAllBytesAsync(caleSalvareCompleta, pdfBytes);

            return caleSalvareCompleta;

        }

        public async Task<string> GenereazaPdfFacturaFurnizorAsync(FacturiFurnizor factura, string numeFurnizor, IEnumerable<(Piese Piesa, TotalPiesaFactura Total)> liniiFactura)
        {
            string folderRelativ = _config["PDFSettings:PathFolderSalvarePDF"] ?? "PdfComenziSalvate";
            string folderAbsolut = Path.Combine(Directory.GetCurrentDirectory(), folderRelativ);

            if (!Directory.Exists(folderAbsolut))
            {
                Console.WriteLine("ATENTIE CA S A CREAT, INSEAMNA CA NU EXISTA");
                Directory.CreateDirectory(folderAbsolut);
            }

            var pdfDocument = new FacturaFurnizorPDFDocument(factura.facturi_id, numeFurnizor, liniiFactura);
            byte[] pdfBytes = pdfDocument.GeneratePdf();

            string numeFisier = $"Factura_{factura.facturi_id}_{numeFurnizor}_{Guid.NewGuid().ToString()[..8]}.pdf";
            string caleSalvareCompleta = Path.Combine(folderAbsolut, numeFisier);

            await File.WriteAllBytesAsync(caleSalvareCompleta, pdfBytes);

            return caleSalvareCompleta;
        }


    }
}