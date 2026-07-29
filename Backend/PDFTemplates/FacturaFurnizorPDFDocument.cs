using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Backend.DBClasses;
using Backend.Endpoints;

namespace Backend.PDFTemplates
{
    public class FacturaFurnizorPDFDocument : IDocument
    {
        private readonly int _facturiId;
        private readonly string _numeFurnizor;
        private readonly IEnumerable<(Piese Piesa, TotalPiesaFactura Total)> _liniiFactura;

        public FacturaFurnizorPDFDocument(int facturiId, string numeFurnizor, IEnumerable<(Piese Piesa, TotalPiesaFactura Total)> liniiFactura)
        {
            _facturiId = facturiId;
            _numeFurnizor = numeFurnizor;
            _liniiFactura = liniiFactura;
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
                        col.Item().Text($"Factură Furnizor #{_facturiId}").Bold().FontSize(18).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Furnizor: {_numeFurnizor}").FontSize(11).Bold();
                    });
                    row.ConstantItem(150).AlignRight().Text($"Data: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9);
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("#");
                            header.Cell().Element(CellStyle).Text("Piesă");
                            header.Cell().Element(CellStyle).Text("Cant.");
                            header.Cell().Element(CellStyle).Text("Preț unitar");
                            header.Cell().Element(CellStyle).Text("Total");

                            static IContainer CellStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.Bold().FontColor(Colors.White))
                                .Background(Colors.Blue.Medium)
                                .Padding(5);
                        });

                        int index = 1;
                        foreach (var (piesa, total) in _liniiFactura)
                        {
                            table.Cell().Element(DataStyle).Text(index++.ToString());
                            table.Cell().Element(DataStyle).Text($"{piesa.nume_piesa}");
                            table.Cell().Element(DataStyle).Text($"{total.CantitateTotala}");
                            table.Cell().Element(DataStyle).Text($"{piesa.pret_cumparare:N2} RON");
                            table.Cell().Element(DataStyle).Text($"{total.PretTotalPiesa:N2} RON");

                            static IContainer DataStyle(IContainer container) => container
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    var totalGeneral = _liniiFactura.Sum(l => l.Total.PretTotalPiesa);

                    col.Item().PaddingTop(15).AlignRight().Text(text =>
                    {
                        text.Span("Total General: ").FontSize(14).Bold();
                        text.Span($"{totalGeneral:N2} RON").FontSize(14).Bold().FontColor(Colors.Red.Medium);
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