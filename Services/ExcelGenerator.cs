using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace CAT.AID.Web.Services.PDF
{
    public abstract class BasePdfTemplate : IDocument
    {
        private readonly string _title;
        private readonly string _logoLeft;
        private readonly string _logoRight;

        protected BasePdfTemplate(string title, string logoLeft, string logoRight)
        {
            _title = title;
            _logoLeft = logoLeft;
            _logoRight = logoRight;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = _title,
            Author = "CAT-AID System"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer()
                    .AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    })
                    ;
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Left logo
                row.ConstantItem(100).Height(60).Element(e =>
                {
                    if (File.Exists(_logoLeft))
                        e.Image(_logoLeft);
                });

                // Title — FIXED (AlignCenter cannot be chained)
                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .AlignCenter()
                        .Text(_title)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                });

                // Right logo
                row.ConstantItem(100).Height(60).Element(e =>
                {
                    if (File.Exists(_logoRight))
                        e.Image(_logoRight);
                });
            });
        }

        public abstract void ComposeContent(IContainer container);
    }
}
