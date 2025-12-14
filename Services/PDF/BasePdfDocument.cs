using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CAT.AID.Web.PDF
{
    public abstract class BasePdfDocument : IDocument
    {
        public string Title { get; set; } = "Comprehensive Vocational Assessment Report";

        public string LogoLeftPath { get; set; }
        public string LogoRightPath { get; set; }

        public byte[]? CandidatePhoto { get; set; }

        // ---------------- CONSTRUCTOR ----------------
        protected BasePdfDocument()
        {
            var root = Directory.GetCurrentDirectory();

            LogoLeftPath  = Path.Combine(root, "wwwroot", "Images", "20240912282747915.png");
            LogoRightPath = Path.Combine(root, "wwwroot", "Images", "202409121913074416.png");

            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ---------------- METADATA ----------------
        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = Title,
            Author = "CAT-AID System",
            Creator = "CAT.AID.Web",
            Producer = "QuestPDF"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // ---------------- DOCUMENT ----------------
        public void Compose(IDocumentContainer doc)
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeBody);
                page.Footer().Element(ComposeFooter);
            });
        }

        public abstract void ComposeBody(IContainer container);

        // ---------------- HEADER ----------------
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Left Logo
                row.ConstantItem(100).Height(55).Element(e =>
                {
                    if (File.Exists(LogoLeftPath))
                        e.Image(LogoLeftPath);
                });

                // Title
                row.RelativeItem().AlignCenter().Text(Title)
                    .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                // Right Logo
                row.ConstantItem(100).Height(55).Element(e =>
                {
                    if (File.Exists(LogoRightPath))
                        e.Image(LogoRightPath);
                });
            });
        }

        // ---------------- FOOTER ----------------
        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            }).FontSize(10);
        }

        // ---------------- SECTION TITLE ----------------
        protected void SectionTitle(IContainer container, string title)
        {
            container.PaddingVertical(8)
                .Text(title)
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        }

        // ---------------- SIGNATURE BLOCK ----------------
        protected void SignatureBlock(IContainer container, string assessorName, string leadName)
        {
            container.PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Assessor Signature: ____________________");
                    col.Item().Text(assessorName).Bold();
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Lead Signature: ________________________");
                    col.Item().Text(leadName).Bold();
                });
            });
        }
    }
}
