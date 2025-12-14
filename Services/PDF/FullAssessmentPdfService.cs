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
            // Convert to absolute paths (Docker-safe)
            LogoLeft  = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "20240912282747915.png");
            LogoRight = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "202409121913074416.png");
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
                a, score, sections, recommendations,
                barChart, doughnutChart,
                LogoLeft, LogoRight);

            return doc.GeneratePdf();
        }
    }

    // ============================================================================
    //                            QUESTPDF DOCUMENT
    // ============================================================================
    public class FullAssessmentReportDocument : BasePdfTemplate
    {
        private readonly Assessment A;
        private readonly AssessmentScoreDTO Score;
        private readonly List<AssessmentSection> Sections;
        private readonly Dictionary<string, List<string>> Recommendations;
        private readonly Dictionary<string, string> Answers;
        private readonly byte[] BarChart;
        private readonly byte[] DoughnutChart;

        private readonly string LeftLogo;
        private readonly string RightLogo;


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
            Score = score ?? new AssessmentScoreDTO();
            Sections = sections ?? new List<AssessmentSection>();
            Recommendations = recommendations ?? new Dictionary<string, List<string>>();

            BarChart = barChart;
            DoughnutChart = doughnutChart;

            Answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson) 
                    ?? new Dictionary<string, string>();

            LeftLogo = leftLogo;
            RightLogo = rightLogo;
        }

        // ============================================================================
        // MAIN CONTENT
        // ============================================================================
        public override void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(col =>
            {
                col.Spacing(20);

                col.Item().Element(CoverPage);
                col.Item().Element(SummarySection);

                if (Recommendations.Any())
                    col.Item().Element(RecommendationsSection);

                col.Item().Element(ChartsSection);

                foreach (var sec in Sections)
                    col.Item().Element(c => SectionBreakdown(c, sec));

                col.Item().Element(EvidenceSection);

                col.Item().Element(SignatureSection);
            });
        }


        // ============================================================================
        // COVER PAGE
        // ============================================================================
        private void CoverPage(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                col.Item().AlignCenter().Text("Comprehensive Vocational Assessment Report")
                    .FontSize(22).Bold().FontColor("#003366");

                col.Item().Text($"Candidate: {A.Candidate.FullName}").FontSize(14).SemiBold();
                col.Item().Text($"DOB: {A.Candidate.DOB:dd-MMM-yyyy}").FontSize(12);
                col.Item().Text($"Gender: {A.Candidate.Gender}").FontSize(12);
                col.Item().Text($"Disability: {A.Candidate.DisabilityType}").FontSize(12);
                col.Item().Text($"Address: {A.Candidate.CommunicationAddress}").FontSize(12);

                if (!string.IsNullOrWhiteSpace(A.Candidate.PhotoFilePath))
                {
                    var imgPath = Path.Combine(Directory.GetCurrentDirectory(), A.Candidate.PhotoFilePath);

                    if (File.Exists(imgPath))
                    {
                        col.Item().PaddingTop(10)
                            .AlignCenter()
                            .Image(imgPath, ImageScaling.FitWidth);
                    }
                }
            });
        }


        // ============================================================================
        // SUMMARY SECTION
        // ============================================================================
        private void SummarySection(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Assessment Summary")
                    .FontSize(18).Bold().FontColor("#003366");

                col.Item().Text($"Total Score: {Score.TotalScore} / {Score.MaxScore}");
                double pct = Score.MaxScore > 0 ? (Score.TotalScore * 100.0 / Score.MaxScore) : 0;

                col.Item().Text($"Percentage: {pct:F1}%");
                col.Item().Text($"Assessment Status: {A.Status}");
                col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}");

                if (Answers.TryGetValue("SUMMARY_COMMENTS", out var summary))
                    col.Item().PaddingTop(5).Text($"Summary Comments:\n{summary}");
            });
        }


        // ============================================================================
        // RECOMMENDATIONS
        // ============================================================================
        private void RecommendationsSection(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Recommendations")
                    .FontSize(18).Bold().FontColor(Colors.Red.Medium);

                foreach (var sec in Recommendations)
                {
                    col.Item().Text(sec.Key).FontSize(14).Bold().FontColor("#003366");

                    col.Item().UnorderedList(list =>
                    {
                        foreach (var rec in sec.Value)
                            list.Item().Text(rec);
                    });
                }
            });
        }


        // ============================================================================
        // CHARTS
        // ============================================================================
        private void ChartsSection(IContainer container)
        {
            container.Row(row =>
            {
                if (BarChart?.Length > 0)
                    row.RelativeItem().Image(BarChart, ImageScaling.FitWidth);

                if (DoughnutChart?.Length > 0)
                    row.RelativeItem().Image(DoughnutChart, ImageScaling.FitWidth);
            });
        }


        // ============================================================================
        // SECTION BREAKDOWN
        // ============================================================================
        private void SectionBreakdown(IContainer container, AssessmentSection sec)
        {
            container.Column(col =>
            {
                col.Item().Text(sec.Category)
                    .FontSize(16).Bold().FontColor("#003366");

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);  // Question
                        c.RelativeColumn(1);  // Score
                        c.RelativeColumn(3);  // Comments
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
                        table.Cell().AlignCenter().Text(scr ?? "0");
                        table.Cell().Text(cmnt ?? "-");
                    }
                });
            });
        }


        // ============================================================================
        // EVIDENCE
        // ============================================================================
        private void EvidenceSection(IContainer container)
        {
            var evidence = Answers
                .Where(k => k.Key.StartsWith("FILE_"))
                .Select(k => k.Value)
                .ToList();

            if (!evidence.Any()) return;

            container.Column(col =>
            {
                col.Item().Text("Evidence Files")
                    .FontSize(16).Bold().FontColor("#003366");

                col.Item().UnorderedList(list =>
                {
                    foreach (var file in evidence)
                        list.Item().Text(file);
                });
            });
        }


        // ============================================================================
        // SIGNATURES
        // ============================================================================
        private void SignatureSection(IContainer container)
        {
            container.PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Assessor Signature").Bold();
                    c.Item().PaddingTop(30).Text("_____________________");
                    c.Item().Text(A.Assessor?.FullName ?? "-");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Lead Assessor Signature").Bold();
                    c.Item().PaddingTop(30).Text("_____________________");
                    c.Item().Text(A.Assessor?.FullName ?? "-");
                });
            });
        }
    }
}
