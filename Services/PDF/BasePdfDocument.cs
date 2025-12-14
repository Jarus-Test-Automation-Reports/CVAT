using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestPDF.Drawing;

namespace CAT.AID.Web.Services.PDF
{
    public abstract class BasePdfDocument : IDocument
    {
        private readonly string _logoLeftPath;
        private readonly string _logoRightPath;

        protected BasePdfDocument(IWebHostEnvironment env)
        {
            _logoLeftPath = Path.Combine(env.WebRootPath, "Images", "20240912282747915.png");
            _logoRightPath = Path.Combine(env.WebRootPath, "Images", "202409121913074416.png");
        }

        public DocumentMetadata GetMetadata()
        {
            return new DocumentMetadata
            {
                Title = "Comprehensive Vocational Assessment Report",
                Author = "CAT-AID Assessment Suite",
                Subject = "Vocational Assessment",
                Producer = "CAT-AID"
            };
        }

        public DocumentSettings GetSettings() => new DocumentSettings
        {
            PdfA = QuestPDF.PdfA.PdfA1A,
            Margins = new PageMargins(40)
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Element(BuildHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }

        // ---------------- HEADER ----------------
        private void BuildHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Width(80).Height(60)
                    .Image(_logoLeftPath, ImageScaling.FitWidth);

                row.RelativeItem().AlignCenter().PaddingTop(10).Text("Comprehensive Vocational Assessment Report")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                row.RelativeItem().AlignRight().Width(80).Height(60)
                    .Image(_logoRightPath, ImageScaling.FitWidth);
            });

            container.PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }

        // ---------------- ABSTRACT CONTENT AREA ----------------
        protected abstract void ComposeContent(IContainer container);
    }
}
