using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace CAT.AID.Web.Services
{
    public class PdfService
    {
        public byte[] Generate(
            string title,
            string candidateName,
            List<(string question, string firstScore, string lastScore, string diff, string notes)> rows)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Header().Column(col =>
                    {
                        col.Item().Text(title)
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();

                        if (!string.IsNullOrWhiteSpace(candidateName))
                        {
                            col.Item().Text($"Candidate: {candidateName}")
                                .FontSize(12)
                                .AlignCenter();
                        }
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(StyleHeader).Text("Question");
                            header.Cell().Element(StyleHeader).Text("1st Score");
                            header.Cell().Element(StyleHeader).Text("Latest Score");
                            header.Cell().Element(StyleHeader).Text("Difference");
                            header.Cell().Element(StyleHeader).Text("Notes");
                        });

                        foreach (var r in rows)
                        {
                            table.Cell().Element(StyleCell).Text(r.question ?? "-");
                            table.Cell().Element(StyleCell).Text(r.firstScore ?? "-");
                            table.Cell().Element(StyleCell).Text(r.lastScore ?? "-");
                            table.Cell().Element(StyleCell).Text(r.diff ?? "-");
                            table.Cell().Element(StyleCell).Text(r.notes ?? "");
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("Page ");
                            txt.CurrentPageNumber();
                            txt.Span(" of ");
                            txt.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer StyleHeader(IContainer container) =>
            container
                .Background("#004080")
                .Padding(5)
                .AlignCenter();
              //  .FontColor(Colors.White)
              //  .Bold();

        private static IContainer StyleCell(IContainer container) =>
            container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten3)
                .PaddingVertical(3)
                .PaddingHorizontal(2);
    }
}
