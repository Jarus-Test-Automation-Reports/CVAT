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
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata();

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Row(row =>
                {
                    row.RelativeItem().AlignLeft().Image(_logoLeft, ImageScaling.FitHeight);
                    row.ConstantItem(300).AlignCenter().Text(_title)
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    row.RelativeItem().AlignRight().Image(_logoRight, ImageScaling.FitHeight);
                });

                page.Content().Section(sec =>
                {
                    sec.Content().Element(ComposeContent);
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Page ");
                    txt.CurrentPageNumber();
                    txt.Span(" of ");
                    txt.TotalPages();
                });
            });
        }

        public abstract void ComposeContent(IContainer container);
    }
}
