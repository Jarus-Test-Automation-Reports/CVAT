using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Text.Json;


namespace CAT.AID.Web.Services.PDF
{
    public class ReportGenerator
    {
        // FIXED: Convert logo paths to full absolute paths
        private static readonly string LogoLeft =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "20240912282747915.png");

        private static readonly string LogoRight =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "202409121913074416.png");

        public static byte[] BuildAssessmentReport(Assessment a)
        {
            // Parse score JSON safely
            var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)
                  ?? new AssessmentScoreDTO();

            // FIXED: absolute path for questions JSON
            var questionFile = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "data", "assessment_questions.json");

            var sections = File.Exists(questionFile)
                ? JsonSerializer.Deserialize<List<AssessmentSection>>(File.ReadAllText(questionFile))
                  ?? new List<AssessmentSection>()
                : new List<AssessmentSection>();

            return new SimpleAssessmentPdf(a, score, sections, LogoLeft, LogoRight)
                .GeneratePdf();
        }
    }

    // =====================================================================
    //                 QUESTPDF DOCUMENT (UNIFORM TEMPLATE)
    // =====================================================================
    public class SimpleAssessmentPdf : BasePdfTemplate
    {
        private readonly Assessment A;
        private readonly AssessmentScoreDTO Score;
        private readonly List<AssessmentSection> Sections;

        public SimpleAssessmentPdf(
            Assessment a,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            string logoLeft,
            string logoRight)
            : base("Assessment Report", logoLeft, logoRight)
        {
            A = a;
            Score = score;
            Sections = sections;
        }

        // ================================================================
        public override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(20);

                col.Item().Element(CandidateSection);
                col.Item().Element(ScoreSummarySection);
                col.Item().Element(SectionScoresTable);
            });
        }

        // ================================================================
        // CANDIDATE DETAILS
        // ================================================================
        private void CandidateSection(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().Text("Candidate Details")
                    .FontSize(18).Bold().FontColor("#003366");

                col.Item().Text($"Name: {A.Candidate.FullName}");
                col.Item().Text($"DOB: {A.Candidate.DOB:dd-MMM-yyyy}");
                col.Item().Text($"Gender: {A.Candidate.Gender}");
                col.Item().Text($"Disability: {A.Candidate.DisabilityType}");
                col.Item().Text($"Address: {A.Candidate.CommunicationAddress}");

                // FIX: Resolve candidate photo path to absolute
                if (!string.IsNullOrWhiteSpace(A.Candidate.PhotoFilePath))
                {
                    var absPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), A.Candidate.PhotoFilePath);

                    if (File.Exists(absPhotoPath))
                    {
                        col.Item().PaddingTop(10)
                            .AlignCenter()
                            .Image(absPhotoPath, ImageScaling.FitWidth);
                    }
                }
            });
        }

        // ================================================================
        // SCORE SUMMARY
        // ================================================================
        private void ScoreSummarySection(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().Text("Assessment Summary")
                    .FontSize(18).Bold().FontColor("#003366");

                col.Item().Text($"Total Score: {Score.TotalScore}");
                col.Item().Text($"Maximum Score: {Score.MaxScore}");

                double pct = Score.MaxScore > 0
                    ? (Score.TotalScore * 100.0 / Score.MaxScore)
                    : 0;

                col.Item().Text($"Percentage: {pct:F1}%");
                col.Item().Text($"Status: {A.Status}");
                col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}");
            });
        }

        // ================================================================
        // SECTION SCORES TABLE
        // ================================================================
        private void SectionScoresTable(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Section Scores")
                    .FontSize(18).Bold().FontColor("#003366");

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // Section Name
                        c.RelativeColumn(1); // Score
                        c.RelativeColumn(1); // Max
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Section").Bold();
                        h.Cell().Text("Score").Bold();
                        h.Cell().Text("Max").Bold();
                    });

                    foreach (var sec in Sections)
                    {
                        Score.SectionScores.TryGetValue(sec.Category, out int scr);
                        int max = sec.Questions.Count * 3;

                        table.Cell().Text(sec.Category);
                        table.Cell().AlignCenter().Text(scr.ToString());
                        table.Cell().AlignCenter().Text(max.ToString());
                    }
                });
            });
        }
    }
}
