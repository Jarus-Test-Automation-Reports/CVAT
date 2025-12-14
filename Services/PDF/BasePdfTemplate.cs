using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Microsoft.AspNetCore.Hosting;

namespace CAT.AID.Web.Services.PDF
{
    public class BasePdfTemplate : IDocument
    {
        private readonly string _title;
        private readonly string _leftLogoPath;
        private readonly string _rightLogoPath;

        public BasePdfTemplate(string title, string leftLogoPath, string rightLogoPath)
        {
            _title = title;
            _leftLogoPath = leftLogoPath;
            _rightLogoPath = rightLogoPath;
        }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata() { Title = _title };

        public DocumentSettings GetSettings() =>
            new DocumentSettings
            {
                PdfAStandard = QuestPDF.Infrastructure.PdfAStandard.None,
                Margins = new QuestPDF.Infrastructure.Margin(25)
            };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(25);

                // HEADER
                page.Header().Element(Header);

                // FOOTER
                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Page ");
                    txt.CurrentPageNumber();
                    txt.Span(" of ");
                    txt.TotalPages();
                }).FontSize(10).FontColor(Colors.Grey.Medium);

                // CONTENT
                page.Content().Element(ComposeContent);
            });
        }

        // Virtual method so individual PDFs can inject their content
        public virtual void ComposeContent(IContainer container)
        {
            container.Text("No content assigned.");
        }

        private void Header(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Height(40).Width(100).Image(_leftLogoPath, ImageScaling.FitArea);
                row.RelativeItem().AlignCenter().Text(_title)
                    .FontSize(18)
                    .SemiBold()
                    .FontColor("#003366");

                row.RelativeItem().AlignRight().Height(40).Width(100).Image(_rightLogoPath, ImageScaling.FitArea);
            });
        }
    }
}
