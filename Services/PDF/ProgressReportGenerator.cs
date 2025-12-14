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

        // ----------------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------------
        public ProgressReportGenerator(Candidate candidate, List<Assessment> history,
            byte[]? barChart = null, byte[]? lineChart = null)
        {
            Candidate = candidate;
            History = history ?? new List<Assessment>();
            BarChart = barChart;
            LineChart = lineChart;
        }

        // ----------------------------------------------------------------------
        // Document Metadata
        // ----------------------------------------------------------------------
        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Progress Assessment Report",
            Author = "CAT-AID System",
            Subject = "Vocational Progress Tracking",
            Keywords = "Assessment, Progress, Candidate"
        };

        // ----------------------------------------------------------------------
        // Document Layout
        // ----------------------------------------------------------------------
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Element(ComposeHeader);

                page.Content().PaddingVertical(10).Element(ComposeBody);

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }

        // ----------------------------------------------------------------------
        // Header (H-A Style)
        // ----------------------------------------------------------------------
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Height(50).Image("wwwroot/Images/20240912282747915.png");

                row.ConstantItem(300).AlignCenter().Stack(stack =>
                {
                    stack.Spacing(2);
                    stack.Text("Progress Assessment Report").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                    stack.Text("Comprehensive Vocational Assessment Report").FontSize(12);
                });

                row.RelativeItem().AlignRight().Height(50).Image("wwwroot/Images/202409121913074416.png");
            });
        }

        // ----------------------------------------------------------------------
        // BODY
        // ----------------------------------------------------------------------
        private void ComposeBody(IContainer container)
        {
            container.Stack(stack =>
            {
                stack.Spacing(15);

                stack.Element(ComposeCandidateInfo);

                stack.Element(ComposeAssessmentOverview);

                stack.Element(ComposeCharts);

                stack.Element(ComposeSectionComparison);

                stack.Element(ComposeStrengthWeakness);

                stack.Element(ComposeRecommendations);

                stack.Element(ComposeSignatures);
            });
        }

        // ----------------------------------------------------------------------
        // Candidate Details Section
        // ----------------------------------------------------------------------
        private void ComposeCandidateInfo(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Candidate Details")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2).Grid(grid =>
                {
                    grid.Columns(2);

                    grid.Item(1).Text(text =>
                    {
                        text.Span("Name: ").SemiBold(); text.Span(Candidate.FullName + "\n");
                        text.Span("Gender: ").SemiBold(); text.Span(Candidate.Gender + "\n");
                        text.Span("DOB: ").SemiBold(); text.Span(Candidate.DOB.ToShortDateString() + "\n");
                        text.Span("Disability Type: ").SemiBold(); text.Span(Candidate.DisabilityType + "\n");
                        text.Span("Education: ").SemiBold(); text.Span(Candidate.Education + "\n");
                        text.Span("Residential Area: ").SemiBold(); text.Span(Candidate.ResidentialArea + "\n");
                        text.Span("Contact: ").SemiBold(); text.Span(Candidate.ContactNumber + "\n");
                        text.Span("Address: ").SemiBold(); text.Span(Candidate.CommunicationAddress + "\n");
                    });

                    grid.Item(1).AlignRight().Width(120).Height(140).Border(1).Image(
                        Candidate.PhotoFilePath ?? "wwwroot/Images/no-photo.png",
                        ImageScaling.FitArea);
                });
            });
        }

        // ----------------------------------------------------------------------
        // Assessment Overview Table
        // ----------------------------------------------------------------------
        private void ComposeAssessmentOverview(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Assessment Overview")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); // Date
                        cols.RelativeColumn(); // Total
                        cols.RelativeColumn(); // Max
                        cols.RelativeColumn(); // %
                        cols.RelativeColumn(); // Status
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Date").SemiBold();
                        h.Cell().Text("Score").SemiBold();
                        h.Cell().Text("Max").SemiBold();
                        h.Cell().Text("Percent").SemiBold();
                        h.Cell().Text("Status").SemiBold();
                    });

                    foreach (var a in History.OrderBy(x => x.CreatedAt))
                    {
                        var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                            ? new AssessmentScoreDTO()
                            : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

                        double pct = score.MaxScore > 0
                            ? Math.Round((double)score.TotalScore / score.MaxScore * 100, 1)
                            : 0;

                        table.Cell().Text(a.CreatedAt.ToShortDateString());
                        table.Cell().Text(score.TotalScore.ToString());
                        table.Cell().Text(score.MaxScore.ToString());
                        table.Cell().Text(pct + "%");
                        table.Cell().Text(a.Status.ToString());
                    }
                });
            });
        }

        // ----------------------------------------------------------------------
        // Charts
        // ----------------------------------------------------------------------
        private void ComposeCharts(IContainer container)
        {
            if ((BarChart == null || BarChart.Length == 0) &&
                (LineChart == null || LineChart.Length == 0))
                return;

            container.Section(section =>
            {
                section.Header().Text("Progress Charts")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().Row(row =>
                {
                    if (BarChart != null)
                        row.RelativeItem().Height(200).Image(BarChart, ImageScaling.FitArea);

                    if (LineChart != null)
                        row.RelativeItem().Height(200).Image(LineChart, ImageScaling.FitArea);
                });
            });
        }

        // ----------------------------------------------------------------------
        // Section-wise Comparison (S1: Table + Cards)
        // ----------------------------------------------------------------------
        private void ComposeSectionComparison(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Section-wise Comparison")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                var latest = History.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                if (latest == null) return;

                var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(latest.ScoreJson);

                // Table
                section.Content().Table(table =>
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

                section.Content().PaddingTop(10).Text("Visual Summary:")
                    .FontSize(12).SemiBold();

                // Cards
                section.Content().Row(row =>
                {
                    row.Spacing(10);

                    foreach (var sec in score.SectionScores)
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Stack(stack =>
                        {
                            stack.Text(sec.Key).SemiBold();
                            stack.Text("Score: " + sec.Value);
                        });
                    }
                });
            });
        }

        // ----------------------------------------------------------------------
        // Strengths & Weakness
        // ----------------------------------------------------------------------
        private void ComposeStrengthWeakness(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Strengths & Weaknesses")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().Row(row =>
                {
                    row.RelativeItem().Border(1).Padding(8).Stack(s =>
                    {
                        s.Text("Strengths").SemiBold();
                        s.Text("• Consistent improvement across domains");
                        s.Text("• Positive learning curve");
                    });

                    row.RelativeItem().Border(1).Padding(8).Stack(s =>
                    {
                        s.Text("Weaknesses").SemiBold();
                        s.Text("• Areas needing targeted training");
                        s.Text("• Requires more reinforcement");
                    });
                });
            });
        }

        // ----------------------------------------------------------------------
        // Recommendations
        // ----------------------------------------------------------------------
        private void ComposeRecommendations(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Recommendations")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().Stack(stack =>
                {
                    stack.Spacing(5);
                    stack.Text("• Provide consistent vocational exposure");
                    stack.Text("• Reinforce skills through practice");
                    stack.Text("• Monitor progress every 3 months");
                });
            });
        }

        // ----------------------------------------------------------------------
        // Signatures
        // ----------------------------------------------------------------------
        private void ComposeSignatures(IContainer container)
        {
            container.Section(section =>
            {
                section.Header().Text("Signatures")
                    .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                section.Content().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Stack(s =>
                    {
                        s.Text("________________________").SemiBold();
                        s.Text("Assessor");
                    });

                    row.RelativeItem().Stack(s =>
                    {
                        s.Text("________________________").SemiBold();
                        s.Text("Lead");
                    });
                });
            });
        }
    }
}
