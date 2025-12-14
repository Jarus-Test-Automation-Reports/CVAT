using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CAT.AID.Web.Services.PDF
{
    public class FullAssessmentPdfService
    {
        public byte[] Generate(
            Assessment a,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            Dictionary<string, List<string>> recommendations,
            byte[] barChart,
            byte[] doughnutChart)
        {
            // Ensure non-null inputs
            a ??= new Assessment { Candidate = new Candidate { FullName = "Unknown" } };
            score ??= new AssessmentScoreDTO();
            sections ??= new List<AssessmentSection>();
            recommendations ??= new Dictionary<string, List<string>>();

            Dictionary<string, string> answers =
                string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new Dictionary<string, string>();

            var model = new PdfModel
            {
                Assessment = a,
                Score = score,
                Sections = sections,
                Recommendations = recommendations,
                Answers = answers,
                BarChart = barChart,
                DoughnutChart = doughnutChart
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Element(c => HeaderSection(c, model));
                    page.Content().Element(c => BodySection(c, model));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(10);
                        x.CurrentPageNumber().FontSize(10);
                    });
                });
            }).GeneratePdf();
        }

        /* ===========================================
           HEADER
        ============================================ */
        private void HeaderSection(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().AlignCenter().Text("ASSESSMENT REPORT")
                    .FontSize(20).Bold();

                col.Item().AlignCenter().Text($"{model.Assessment.Candidate.FullName}")
                    .FontSize(12);

                col.Item().AlignCenter().Text($"Submitted: {model.Assessment.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}")
                    .FontSize(10);

                col.Item().PaddingTop(10).LineHorizontal(1);
            });
        }

        /* ===========================================
           BODY
        ============================================ */
        private void BodySection(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(25);

                col.Item().Element(c => SummaryBlock(c, model));
                col.Item().Element(c => RecommendationsBlock(c, model));
                col.Item().Element(c => SectionBreakdown(c, model));
                col.Item().Element(c => ChartsBlock(c, model));
            });
        }

        /* ===========================================
           SUMMARY BLOCK
        ============================================ */
        private void SummaryBlock(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().Text("📌 SUMMARY").FontSize(14).Bold();

                double pct = model.Score.MaxScore > 0
                    ? Math.Round((double)model.Score.TotalScore / model.Score.MaxScore * 100, 2)
                    : 0;

                col.Item().Text($"Total Score: {model.Score.TotalScore} / {model.Score.MaxScore}");
                col.Item().Text($"Percentage: {pct}%");
                col.Item().Text($"Status: {model.Assessment.Status}");
            });
        }

        /* ===========================================
           RECOMMENDATIONS
        ============================================ */
        private void RecommendationsBlock(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("🎯 RECOMMENDATIONS").FontSize(14).Bold();

                if (!model.Recommendations.Any())
                {
                    col.Item().Text("No recommendations — strong performance.").Bold().FontColor(Colors.Green.Medium);
                    return;
                }

                foreach (var sec in model.Recommendations)
                {
                    col.Item().Text(sec.Key).Bold().FontColor(Colors.Red.Darken2);

                    col.Item().List(list =>
                    {
                        foreach (var entry in sec.Value)
                            list.Item().Text(entry);
                    });
                }
            });
        }

        /* ===========================================
           SECTION QUESTION BREAKDOWN
        ============================================ */
        private void SectionBreakdown(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(15);
                col.Item().Text("📑 SECTION BREAKDOWN").FontSize(14).Bold();

                foreach (var sec in model.Sections)
                {
                    col.Item().Text(sec.Category).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(260);
                            c.ConstantColumn(40);
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Question").Bold();
                            h.Cell().Text("Score").Bold();
                            h.Cell().Text("Comments").Bold();
                        });

                        foreach (var q in sec.Questions)
                        {
                            model.Answers.TryGetValue($"SCORE_{q.Id}", out string scr);
                            model.Answers.TryGetValue($"CMT_{q.Id}", out string cmt);

                            table.Cell().Text(q.Text);
                            table.Cell().AlignCenter().Text(scr ?? "0");
                            table.Cell().Text(cmt ?? "-");
                        }
                    });
                }
            });
        }

        /* ===========================================
           CHART IMAGES
        ============================================ */
        private void ChartsBlock(IContainer container, PdfModel model)
        {
            container.Column(col =>
            {
                col.Spacing(20);

                if (model.BarChart?.Length > 0)
                {
                    col.Item().AlignCenter().Image(model.BarChart);
                }

                if (model.DoughnutChart?.Length > 0)
                {
                    col.Item().AlignCenter().Image(model.DoughnutChart);
                }
            });
        }

        /* ===========================================
           INTERNAL MODEL
        ============================================ */
        private class PdfModel
        {
            public Assessment Assessment { get; set; }
            public AssessmentScoreDTO Score { get; set; }
            public List<AssessmentSection> Sections { get; set; }
            public Dictionary<string, List<string>> Recommendations { get; set; }
            public Dictionary<string, string> Answers { get; set; }
            public byte[] BarChart { get; set; }
            public byte[] DoughnutChart { get; set; }
        }
    }
}
