using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend.PDFTemplates
{
    public class ComandaPdfDocument : IDocument
    {
        private readonly int _idComanda;
        private readonly string _numeCompanie;
        private readonly decimal _totalGeneral;
        private readonly IEnumerable<dynamic> _pieseComandate;

        public ComandaPdfDocument(int idComanda, string numeCompanie, decimal totalGeneral, IEnumerable<dynamic> pieseComandate)
        {
            _idComanda = idComanda;
            _numeCompanie = numeCompanie;
            _totalGeneral = totalGeneral;
            _pieseComandate = pieseComandate;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

     
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Comandă #{_idComanda}").Bold().FontSize(18).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Companie: {_numeCompanie}").FontSize(11).Bold();
                    });
                    row.ConstantItem(150).AlignRight().Text($"Data: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9);
                });


                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantItem(25);  
                            columns.RelativeItem(2.5f);
                            columns.RelativeItem(2);  
                            columns.RelativeItem(2);   
                            columns.RelativeItem(0.8f);
                            columns.RelativeItem(1.2f); 
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("#");
                            header.Cell().Element(CellStyle).Text("Piesă");
                            header.Cell().Element(CellStyle).Text("Furnizor");
                            header.Cell().Element(CellStyle).Text("Detalii / Comentariu");
                            header.Cell().Element(CellStyle).Text("Cant.");
                            header.Cell().Element(CellStyle).Text("Total");

                            static IContainer CellStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.Bold().FontColor(Colors.White))
                                .Background(Colors.Blue.Medium)
                                .Padding(5);
                        });

                        int index = 1;
                        foreach (var item in _pieseComandate)
                        {
                            table.Cell().Element(DataStyle).Text(index++.ToString());
                            table.Cell().Element(DataStyle).Text(item.Piesa.nume_piesa);
                            table.Cell().Element(DataStyle).Column(c =>
                            {
                                c.Item().Text(item.FurnizorPiesa.nume_furnizor).Bold();
                                c.Item().Text(item.FurnizorPiesa.numar_telefon).FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                            table.Cell().Element(DataStyle).Text(item.DetaliiComandaPiesa.detalii_piese ?? "-");
                            table.Cell().Element(DataStyle).Text(item.DetaliiComandaPiesa.cantitate_comandata.ToString());
                            table.Cell().Element(DataStyle).Text($"{item.PretPiese:N2} RON");

                            static IContainer DataStyle(IContainer container) => container
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Text(text =>
                    {
                        text.Span("Total General: ").FontSize(14).Bold();
                        text.Span($"{_totalGeneral:N2} RON").FontSize(14).Bold().FontColor(Colors.Red.Medium);
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ");
                    x.CurrentPageNumber();
                    x.Span(" din ");
                    x.TotalPages();
                });
            });
        }
    }
}