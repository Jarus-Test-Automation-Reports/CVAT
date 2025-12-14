using CAT.AID.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CAT.AID.Web.Services.PDF
{
    public class ProgressReportGenerator : IDocument
    {
        private readonly Candidate Candidate;
        private readonly List<Assessment> History;
        private readonly byte[]? BarChart;
        private readonly byte[]? LineChart;

        public ProgressReportGenerator(
            Candidate candidate,
            List<Assessment> history,
            byte[]? barChart = null,
            byte[]? lineChart = null)
        {
            Candidate = candidate;
            History = history ?? new List<Assessment>();
            BarChart = barChart;
            LineChart = lineChart;
        }

        // Metadata
        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Progress Assessment Report",
            Author = "CAT-AID System"
        };

        // Document root
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Element(ComposeHeader);

                page.Content().PaddingVertical(10).Element(ComposeBody);

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }

        // ------------------------------------------------------------
        // HEADER
        // ------------------------------------------------------------
        private void ComposeHeader(IContainer container)
        {
            var logoLeft  = Path.Combine("wwwroot", "Images", "20240912282747915.png");
            var logoRight = Path.Combine("wwwroot", "Images", "202409121913074416.png");

            container.Row(row =>
            {
                row.RelativeItem().Height(50).AlignLeft().Element(x =>
                {
                    if (File.Exists(logoLeft))
                        x.Image(logoLeft);
                });

                row.ConstantItem(300).AlignCenter().Column(col =>
                {
                    col.Item().Text("Progress Assessment Report")
                        .FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);

                    col.Item().Text("Comprehensive Vocational Assessment Report")
                        .FontSize(12);
                });

                row.RelativeItem().Height(50).AlignRight().Element(x =>
                {
                    if (File.Exists(logoRight))
                        x.Image(logoRight);
                });
            });
        }

        // ------------------------------------------------------------
        // BODY CONTENT
        // ------------------------------------------------------------
        private void ComposeBody(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(15);

                col.Item().Element(ComposeCandidateInfo);
                col.Item().Element(ComposeAssessmentOverview);
                col.Item().Element(ComposeCharts);
                col.Item().Element(ComposeSectionComparison);
                col.Item().Element(ComposeStrengthWeakness);
                col.Item().Element(ComposeRecommendations);
                col.Item().Element(ComposeSignatures);
            });
        }

        // ------------------------------------------------------------
        // CANDIDATE SECTION
        // ------------------------------------------------------------
        private void ComposeCandidateInfo(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                .Column(col =>
                {
                    col.Item().Text("Candidate Details")
                        .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t =>
                            {
                                t.Span("Name: ").SemiBold();            t.Span(Candidate.FullName + "\n");
                                t.Span("Gender: ").SemiBold();          t.Span(Candidate.Gender + "\n");
                                t.Span("DOB: ").SemiBold();             t.Span(Candidate.DOB.ToShortDateString() + "\n");
                                t.Span("Disability: ").SemiBold();      t.Span(Candidate.DisabilityType + "\n");
                                t.Span("Education: ").SemiBold();       t.Span(Candidate.Education + "\n");
                                t.Span("Area: ").SemiBold();            t.Span(Candidate.ResidentialArea + "\n");
                                t.Span("Contact: ").SemiBold();         t.Span(Candidate.ContactNumber + "\n");
                                t.Span("Address: ").SemiBold();         t.Span(Candidate.CommunicationAddress + "\n");
                            });
                        });

                        row.ConstantItem(120).Height(140).Border(1).Element(img =>
                        {
                            var path = Candidate.PhotoFilePath ?? "wwwroot/Images/no-photo.png";
                            if (File.Exists(path))
                                img.Image(path);
                        });
                    });
                });
        }

        // ------------------------------------------------------------
        // OVERVIEW TABLE
        // ------------------------------------------------------------
        private void ComposeAssessmentOverview(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Assessment Overview")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Date").SemiBold();
                        header.Cell().Text("Score").SemiBold();
                        header.Cell().Text("Max").SemiBold();
                        header.Cell().Text("%").SemiBold();
                        header.Cell().Text("Status").SemiBold();
                    });

                    foreach (var a in History.OrderBy(x => x.CreatedAt))
                    {
                        var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                            ? new AssessmentScoreDTO()
                            : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)!;

                        double pct = score.MaxScore > 0 ? Math.Round(score.TotalScore * 100.0 / score.MaxScore, 1) : 0;

                        table.Cell().Text(a.CreatedAt.ToShortDateString());
                        table.Cell().Text(score.TotalScore.ToString());
                        table.Cell().Text(score.MaxScore.ToString());
                        table.Cell().Text($"{pct}%");
                        table.Cell().Text(a.Status.ToString());
                    }
                });
            });
        }

        // ------------------------------------------------------------
        // CHARTS
        // ------------------------------------------------------------
        private void ComposeCharts(IContainer container)
        {
            if (BarChart == null && LineChart == null)
                return;

            container.Column(col =>
            {
                col.Item().Text("Progress Charts")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                col.Item().Row(row =>
                {
                    if (BarChart != null)
                        row.RelativeItem().Height(200).Image(BarChart);

                    if (LineChart != null)
                        row.RelativeItem().Height(200).Image(LineChart);
                });
            });
        }

        // ------------------------------------------------------------
        // SECTION COMPARISON
        // ------------------------------------------------------------
        private void ComposeSectionComparison(IContainer container)
        {
            var latest = History.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latest == null) return;

            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(latest.ScoreJson);
            if (score == null || score.SectionScores == null) return;

            container.Column(col =>
            {
                col.Item().Text("Section-wise Comparison")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                // table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Section").SemiBold();
                        h.Cell().Text("Score").SemiBold();
                    });

                    foreach (var sec in score.SectionScores)
                    {
                        table.Cell().Text(sec.Key);
                        table.Cell().Text(sec.Value.ToString());
                    }
                });

                // visual cards
                col.Item().PaddingTop(10).Text("Visual Summary:").SemiBold();

                col.Item().Row(row =>
                {
                    row.Spacing(10);

                    foreach (var sec in score.SectionScores)
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Column(c =>
                            {
                                c.Item().Text(sec.Key).SemiBold();
                                c.Item().Text("Score: " + sec.Value);
                            });
                    }
                });
            });
        }

        // ------------------------------------------------------------
        // STRENGTH & WEAKNESS
        // ------------------------------------------------------------
        private void ComposeStrengthWeakness(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Border(1).Padding(8).Column(col =>
                {
                    col.Item().Text("Strengths").SemiBold();
                    col.Item().Text("• Consistent improvement");
                    col.Item().Text("• Strong learning curve");
                });

                row.RelativeItem().Border(1).Padding(8).Column(col =>
                {
                    col.Item().Text("Weaknesses").SemiBold();
                    col.Item().Text("• Needs reinforcement in difficult tasks");
                    col.Item().Text("• Areas requiring targeted training");
                });
            });
        }

        // ------------------------------------------------------------
        // RECOMMENDATIONS
        // ------------------------------------------------------------
        private void ComposeRecommendations(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Recommendations")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                col.Item().Text("• Provide vocational exposure");
                col.Item().Text("• Reinforce through structured practice");
                col.Item().Text("• Monitor every 3 months");
            });
        }

        // ------------------------------------------------------------
        // SIGNATURES
        // ------------------------------------------------------------
        private void ComposeSignatures(IContainer container)
        {
            container.PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("________________________").SemiBold();
                    c.Item().Text("Assessor");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("________________________").SemiBold();
                    c.Item().Text("Lead Assessor");
                });
            });
        }
    }
}
