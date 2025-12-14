using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CAT.AID.Web.PDF
{
    public abstract class BasePdfDocument : IDocument
    {
        public string Title { get; set; } = "Comprehensive Vocational Assessment Report";

        // Absolute paths (Docker-safe)
        public string LogoLeftPath { get; set; }
        public string LogoRightPath { get; set; }

        // Optional candidate photo bytes
        public byte[]? CandidatePhoto { get; set; }

        // ---------------- CONSTRUCTOR ----------------
        protected BasePdfDocument()
        {
            var root = Directory.GetCurrentDirectory();

            LogoLeftPath  = Path.Combine(root, "wwwroot", "Images", "20240912282747915.png");
            LogoRightPath = Path.Combine(root, "wwwroot", "Images", "202409121913074416.png");

            // Register a Linux-safe Unicode font (supports Telugu)
            QuestPDF.Settings.License = LicenseType.Community;
            FontManager.RegisterFont(File.OpenRead(Path.Combine(root, "wwwroot", "fonts", "NotoSans-Regular.ttf")));
            FontManager.RegisterFont(File.OpenRead(Path.Combine(root, "wwwroot", "fonts", "NotoSans-Bold.ttf")));
        }

        // ---------------- METADATA ----------------
        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = Title,
            Author = "CAT-AID System",
            Producer = "QuestPDF",
            Creator = "CAT.AID.Web"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // ---------------------------------------------------------
        // MAIN DOCUMENT COMPOSER
        // ---------------------------------------------------------
        public void Compose(IDocumentContainer doc)
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("NotoSans").FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeBody);
                page.Footer().Element(ComposeFooter);
            });
        }

        // Must be implemented by each PDF subtype
        public abstract void ComposeBody(IContainer container);

        // ---------------------------------------------------------
        // HEADER
        // ---------------------------------------------------------
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Left logo
                row.ConstantItem(100).Height(55).Element(x =>
                {
                    if (File.Exists(LogoLeftPath))
                        x.Image(LogoLeftPath, ImageScaling.FitArea);
                });

                // Title
                row.RelativeItem().AlignCenter().Text(Title)
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                // Right logo
                row.ConstantItem(100).Height(55).Element(x =>
                {
                    if (File.Exists(LogoRightPath))
                        x.Image(LogoRightPath, ImageScaling.FitArea);
                });
            });
        }

        // ---------------------------------------------------------
        // FOOTER
        // ---------------------------------------------------------
        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(t =>
            {
                t.Span("Page ").FontSize(10);
                t.CurrentPageNumber().FontSize(10);
                t.Span(" of ").FontSize(10);
                t.TotalPages().FontSize(10);
            });
        }

        // ---------------------------------------------------------
        // COMMON HEADING STYLE
        // ---------------------------------------------------------
        protected IContainer SectionTitle(IContainer container, string title)
        {
            return container.PaddingVertical(8).Text(title)
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        }

        // ---------------------------------------------------------
        // SIGNATURE BLOCK
        // ---------------------------------------------------------
        protected void SignatureBlock(
            IContainer container,
            string assessorName,
            string leadName)
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
