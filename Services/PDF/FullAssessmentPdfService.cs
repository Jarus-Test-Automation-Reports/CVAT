using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Text.Json;

namespace CAT.AID.Web.Services.PDF
{
    public class FullAssessmentPdfService
    {
        private readonly string LogoLeft;
        private readonly string LogoRight;

        public FullAssessmentPdfService()
        {
            var root = Directory.GetCurrentDirectory();
            LogoLeft  = Path.Combine(root, "wwwroot", "Images", "20240912282747915.png");
            LogoRight = Path.Combine(root, "wwwroot", "Images", "202409121913074416.png");

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(
            Assessment a,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            Dictionary<string, List<string>> recommendations,
            byte[] barChart,
            byte[] doughnutChart)
        {
            var doc = new FullAssessmentReportDocument(
                a,
                score ?? new AssessmentScoreDTO(),
                sections ?? new List<AssessmentSection>(),
                recommendations ?? new Dictionary<string, List<string>>(),
                barChart ?? Array.Empty<byte>(),
                doughnutChart ?? Array.Empty<byte>(),
                LogoLeft,
                LogoRight
            );

            return doc.GeneratePdf();
        }
    }

    // ----------------------------------------------------------------------
    //                         QUESTPDF DOCUMENT
    // ----------------------------------------------------------------------
    public class FullAssessmentReportDocument : BasePdfTemplate
    {
        private readonly Assessment A;
        private readonly AssessmentScoreDTO Score;
        private readonly List<AssessmentSection> Sections;
        private readonly Dictionary<string, List<string>> Recommendations;
        private readonly Dictionary<string, string> Answers;

        private readonly byte[] BarChart;
        private readonly byte[] DoughnutChart;

        public FullAssessmentReportDocument(
            Assessment a,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            Dictionary<string, List<string>> recommendations,
            byte[] barChart,
            byte[] doughnutChart,
            string leftLogo,
            string rightLogo)
            : base("Comprehensive Vocational Assessment Report", leftLogo, rightLogo)
        {
            A = a;
            Score = score;
            Sections = sections;
            Recommendations = recommendations;

            BarChart = barChart;
            DoughnutChart = doughnutChart;

            Answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new();
        }

        // MAIN CONTENT --------------------------------------------------------
        public override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(20);

                col.Item().Element(CandidateInfo);
                col.Item().Element(Summary);
                if (Recommendations.Any())
                    col.Item().Element(RecommendationsBlock);

                col.Item().Element(ChartsBlock);

                foreach (var sec in Sections)
                    col.Item().Element(c => SectionTable(c, sec));

                col.Item().Element(EvidenceBlock);
                col.Item().Element(SignatureBlock);
            });
        }

        // ----------------------------------------------------------------------
        // CANDIDATE DETAILS
        // ----------------------------------------------------------------------
        private void CandidateInfo(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Candidate Details").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Name: {A.Candidate.FullName}");
                col.Item().Text($"DOB: {A.Candidate.DOB:dd-MMM-yyyy}");
                col.Item().Text($"Gender: {A.Candidate.Gender}");
                col.Item().Text($"Disability: {A.Candidate.DisabilityType}");
                col.Item().Text($"Address: {A.Candidate.CommunicationAddress}");

                if (!string.IsNullOrWhiteSpace(A.Candidate.PhotoFilePath))
                {
                    var abs = Path.Combine(Directory.GetCurrentDirectory(), A.Candidate.PhotoFilePath);
                    if (File.Exists(abs))
                        col.Item().PaddingTop(10).Image(abs, ImageScaling.FitWidth);
                }
            });
        }

        // ----------------------------------------------------------------------
        // SUMMARY
        // ----------------------------------------------------------------------
        private void Summary(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Assessment Summary").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                double pct = Score.MaxScore == 0 ? 0 : Score.TotalScore * 100.0 / Score.MaxScore;

                col.Item().Text($"Total Score: {Score.TotalScore} / {Score.MaxScore}");
                col.Item().Text($"Percentage: {pct:F1}%");
                col.Item().Text($"Status: {A.Status}");
                col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}");

                if (Answers.TryGetValue("SUMMARY_COMMENTS", out var summary))
                    col.Item().Text($"Comments:\n{summary}");
            });
        }

        // ----------------------------------------------------------------------
        // RECOMMENDATIONS
        // ----------------------------------------------------------------------
        private void RecommendationsBlock(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Recommendations")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                foreach (var sec in Recommendations)
                {
                    col.Item().Text(sec.Key).Bold();
                    col.Item().Column(list =>
                    {
                        foreach (var rec in sec.Value)
                            list.Item().Text("• " + rec);
                    });
                }
            });
        }

        // ----------------------------------------------------------------------
        // CHARTS
        // ----------------------------------------------------------------------
        private void ChartsBlock(IContainer container)
        {
            if (BarChart.Length == 0 && DoughnutChart.Length == 0)
                return;

            container.Column(col =>
            {
                col.Item().Text("Assessment Visual Charts")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                col.Item().Row(row =>
                {
                    if (BarChart.Length > 0)
                        row.RelativeItem().Height(180).Image(BarChart, ImageScaling.FitArea);

                    if (DoughnutChart.Length > 0)
                        row.RelativeItem().Height(180).Image(DoughnutChart, ImageScaling.FitArea);
                });
            });
        }

        // ----------------------------------------------------------------------
        // SECTION TABLE
        // ----------------------------------------------------------------------
        private void SectionTable(IContainer container, AssessmentSection sec)
        {
            container.Column(col =>
            {
                col.Item().Text(sec.Category)
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(3);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Question").Bold();
                        h.Cell().Text("Score").Bold();
                        h.Cell().Text("Comments").Bold();
                    });

                    foreach (var q in sec.Questions)
                    {
                        Answers.TryGetValue($"SCORE_{q.Id}", out var scr);
                        Answers.TryGetValue($"CMT_{q.Id}", out var cmnt);

                        table.Cell().Text(q.Text);
                        table.Cell().Text(scr ?? "0");
                        table.Cell().Text(cmnt ?? "-");
                    }
                });
            });
        }

        // ----------------------------------------------------------------------
        // EVIDENCE LIST
        // ----------------------------------------------------------------------
        private void EvidenceBlock(IContainer container)
        {
            var files = Answers
                .Where(a => a.Key.StartsWith("FILE_"))
                .Select(a => a.Value)
                .ToList();

            if (!files.Any())
                return;

            container.Column(col =>
            {
                col.Item().Text("Evidence Files")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                col.Item().Column(list =>
                {
                    foreach (var f in files)
                        list.Item().Text(f);
                });
            });
        }

        // ----------------------------------------------------------------------
        // SIGNATURES
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
// SIGNATURES
// ----------------------------------------------------------------------
private void SignatureBlock(IContainer container)
{
    container.Column(col =>
    {
        col.Item().Text("Signatures")
            .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

        col.Item().Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Assessor").Bold();
                c.Item().Text("____________________");
                c.Item().Text(A.Assessor?.FullName ?? "-");
            });

            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Lead Assessor").Bold();
                c.Item().Text("____________________");
                c.Item().Text(A.LeadAssessor?.FullName ?? "-");
            });
        });
    });
}

    }
}

