using CAT.AID.Models;
using CAT.AID.Models.DTO;
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

        private readonly string LogoLeft;
        private readonly string LogoRight;
        private readonly string NoPhoto;

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

            // Absolute paths (Docker-safe)
            var root = Directory.GetCurrentDirectory();
            LogoLeft  = Path.Combine(root, "wwwroot", "Images", "20240912282747915.png");
            LogoRight = Path.Combine(root, "wwwroot", "Images", "202409121913074416.png");
            NoPhoto   = Path.Combine(root, "wwwroot", "Images", "no-photo.png");

            // QuestPDF Licensing (required, but NO FONT REGISTRATION!)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Progress Assessment Report",
            Author = "CAT-AID System",
            Creator = "CAT.AID.Web",
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // ------------------------------------------------------------
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                // IMPORTANT: NotoSans must exist in system fonts (you already copied into /usr/share/fonts) 
                page.DefaultTextStyle(x => x.FontFamily("Noto Sans").FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().PaddingVertical(10).Element(ComposeBody);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }

        // ------------------------------------------------------------
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Height(55).Element(x =>
                {
                    if (File.Exists(LogoLeft))
                        x.Image(LogoLeft, ImageScaling.FitArea);
                });

                row.RelativeItem().AlignCenter().Column(col =>
                {
                    col.Item().Text("Progress Assessment Report")
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Comprehensive Vocational Assessment Tracking")
                        .FontSize(12);
                });

                row.RelativeItem().Height(55).Element(x =>
                {
                    if (File.Exists(LogoRight))
                        x.Image(LogoRight, ImageScaling.FitArea);
                });
            });
        }

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
        private void ComposeCandidateInfo(IContainer container)
        {
            var photo = Candidate.PhotoFilePath;
            var photoPath = string.IsNullOrWhiteSpace(photo)
                ? NoPhoto
                : Path.Combine(Directory.GetCurrentDirectory(), photo);

            if (!File.Exists(photoPath))
                photoPath = NoPhoto;

            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                .Column(col =>
                {
                    col.Item().Text("Candidate Details")
                        .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(info =>
                        {
                            info.Item().Text($"Name: {Candidate.FullName}");
                            info.Item().Text($"Gender: {Candidate.Gender}");
                            info.Item().Text($"DOB: {Candidate.DOB:dd-MMM-yyyy}");
                            info.Item().Text($"Disability: {Candidate.DisabilityType}");
                            info.Item().Text($"Education: {Candidate.Education}");
                            info.Item().Text($"Area: {Candidate.ResidentialArea}");
                            info.Item().Text($"Contact: {Candidate.ContactNumber}");
                            info.Item().Text($"Address: {Candidate.CommunicationAddress}");
                        });

                        row.ConstantItem(120).Height(140).Border(1).Padding(3)
                            .Image(photoPath, ImageScaling.FitArea);
                    });
                });
        }

        // ------------------------------------------------------------
        private void ComposeAssessmentOverview(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Assessment Overview")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

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

                    table.Header(h =>
                    {
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Score").Bold();
                        h.Cell().Text("Max").Bold();
                        h.Cell().Text("%").Bold();
                        h.Cell().Text("Status").Bold();
                    });

                    foreach (var item in History.OrderBy(x => x.CreatedAt))
                    {
                        var score = string.IsNullOrWhiteSpace(item.ScoreJson)
                            ? new AssessmentScoreDTO()
                            : JsonSerializer.Deserialize<AssessmentScoreDTO>(item.ScoreJson)
                                ?? new AssessmentScoreDTO();

                        double pct = score.MaxScore == 0 ? 0 : Math.Round(score.TotalScore * 100.0 / score.MaxScore, 1);

                        table.Cell().Text(item.CreatedAt.ToShortDateString());
                        table.Cell().Text(score.TotalScore.ToString());
                        table.Cell().Text(score.MaxScore.ToString());
                        table.Cell().Text($"{pct}%");
                        table.Cell().Text(item.Status.ToString());
                    }
                });
            });
        }

        // ------------------------------------------------------------
        private void ComposeCharts(IContainer container)
        {
            if (BarChart == null && LineChart == null)
                return;

            container.Column(col =>
            {
                col.Item().Text("Progress Charts")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

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
        private void ComposeSectionComparison(IContainer container)
        {
            var latest = History.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latest == null || string.IsNullOrWhiteSpace(latest.ScoreJson))
                return;

            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(latest.ScoreJson);
            if (score?.SectionScores == null)
                return;

            container.Column(col =>
            {
                col.Item().Text("Section-wise Comparison")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Section").Bold();
                        h.Cell().Text("Score").Bold();
                    });

                    foreach (var sec in score.SectionScores)
                    {
                        table.Cell().Text(sec.Key);
                        table.Cell().Text(sec.Value.ToString());
                    }
                });

                col.Item().PaddingTop(10).Text("Visual Summary:").SemiBold();

                col.Item().Row(row =>
                {
                    foreach (var sec in score.SectionScores)
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(8).Column(c =>
                            {
                                c.Item().Text(sec.Key).SemiBold();
                                c.Item().Text($"Score: {sec.Value}");
                            });
                    }
                });
            });
        }

        // ------------------------------------------------------------
        private void ComposeStrengthWeakness(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Border(1).Padding(8).Column(c =>
                {
                    c.Item().Text("Strengths").Bold();
                    c.Item().Text("• Consistent improvement");
                    c.Item().Text("• Positive learning curve");
                });

                row.RelativeItem().Border(1).Padding(8).Column(c =>
                {
                    c.Item().Text("Weaknesses").Bold();
                    c.Item().Text("• Requires reinforcement");
                    c.Item().Text("• Needs targeted training");
                });
            });
        }

        // ------------------------------------------------------------
        private void ComposeRecommendations(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Recommendations")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                col.Item().Text("• Provide regular vocational exposure");
                col.Item().Text("• Reinforce practical skills");
                col.Item().Text("• Monitor progress every 3 months");
            });
        }

        // ------------------------------------------------------------
        private void ComposeSignatures(IContainer container)
        {
            container.PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("________________________");
                    c.Item().Text("Assessor");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("________________________");
                    c.Item().Text("Lead Assessor");
                });
            });
        }
    }
}
