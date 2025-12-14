using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CAT.AID.Web.PDF
{
    public abstract class BasePdfDocument : IDocument
    {
        public string Title { get; set; } = "Comprehensive Vocational Assessment Report";

        // logos
        public string LogoLeftPath { get; set; } = "wwwroot/Images/20240912282747915.png";
        public string LogoRightPath { get; set; } = "wwwroot/Images/202409121913074416.png";

        // candidate photo optional
        public byte[]? CandidatePhoto { get; set; }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = Title,
            Author = "CVAT System",
            Creator = "CAT.AID.Web",
            Producer = "QuestPDF",
        };

        public abstract void ComposeBody(IContainer container);

        public DocumentSettings GetSettings() => DocumentSettings.Default;

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

        // ---------------- HEADER ----------------
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Left logo
                row.RelativeItem(1).AlignLeft().Height(60).Width(100).Image(LogoLeftPath);

                // Title in center
                row.RelativeItem(3).AlignCenter().Column(col =>
                {
                    col.Item().Text(Title)
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                });

                // Right logo
                row.RelativeItem(1).AlignRight().Height(60).Width(100).Image(LogoRightPath);
            });
        }

        // ---------------- FOOTER ----------------
        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("Page ").FontSize(10);
                text.CurrentPageNumber().FontSize(10);
                text.Span(" of ").FontSize(10);
                text.TotalPages().FontSize(10);
            });
        }

        // ---------------- SHARED HEADING STYLE ----------------
        protected IContainer SectionTitle(IContainer container, string title)
        {
            return container.PaddingVertical(8).Text(title)
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        }

        // ---------------- SIGNATURE BLOCK (Style C) ----------------
        protected void SignatureBlock(IContainer container, 
            string assessorName, 
            string leadName)
        {
            container.PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().PaddingBottom(40).Text("Assessor Signature: ____________________");
                    col.Item().Text(assessorName).Bold();
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().PaddingBottom(40).Text("Lead Signature: ________________________");
                    col.Item().Text(leadName).Bold();
                });
            });
        }
    }
}
