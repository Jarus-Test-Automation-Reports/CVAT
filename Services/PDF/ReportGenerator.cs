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
        private const string LogoLeft = "wwwroot/Images/20240912282747915.png";
        private const string LogoRight = "wwwroot/Images/202409121913074416.png";

        public static byte[] BuildAssessmentReport(Assessment a)
        {
            // Parse score
            var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson) ?? new AssessmentScoreDTO();

            // Load questions for section counts
            var questionFile = Path.Combine("wwwroot", "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(File.ReadAllText(questionFile))
                           ?? new List<AssessmentSection>();

            return new SimpleAssessmentPdf(a, score, sections, LogoLeft, LogoRight).GeneratePdf();
        }
    }

    // =====================================================================
    //                     QUESTPDF DOCUMENT (UNIFORM TEMPLATE)
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

        // --------------------------------------------------------------
        // CANDIDATE DETAILS
        // --------------------------------------------------------------
        private void CandidateSection(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().Text("Candidate Details")
                    .FontSize(18)
                    .Bold()
                    .FontColor("#003366");

                col.Item().Text($"Name: {A.Candidate.FullName}").FontSize(12);
                col.Item().Text($"DOB: {A.Candidate.DOB:dd-MMM-yyyy}").FontSize(12);
                col.Item().Text($"Gender: {A.Candidate.Gender}").FontSize(12);
                col.Item().Text($"Disability: {A.Candidate.DisabilityType}").FontSize(12);
                col.Item().Text($"Address: {A.Candidate.CommunicationAddress}").FontSize(12);

                if (!string.IsNullOrWhiteSpace(A.Candidate.PhotoFilePath))
                {
                    col.Item().PaddingTop(10)
                        .Image(A.Candidate.PhotoFilePath, ImageScaling.FitWidth);
                }
            });
        }

        // --------------------------------------------------------------
        // SCORE SUMMARY
        // --------------------------------------------------------------
        private void ScoreSummarySection(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(5);

                col.Item().Text("Assessment Summary")
                    .FontSize(18)
                    .Bold()
                    .FontColor("#003366");

                col.Item().Text($"Total Score: {Score.TotalScore}").FontSize(12);
                col.Item().Text($"Maximum Score: {Score.MaxScore}").FontSize(12);

                double pct = Score.MaxScore > 0
                    ? (Score.TotalScore * 100.0 / Score.MaxScore)
                    : 0;

                col.Item().Text($"Percentage: {pct:F1}%").FontSize(12);

                col.Item().Text($"Status: {A.Status}").FontSize(12);
                col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}")
                    .FontSize(12);
            });
        }

        // --------------------------------------------------------------
        // SECTION SCORES TABLE
        // --------------------------------------------------------------
        private void SectionScoresTable(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Section Scores")
                    .FontSize(18)
                    .Bold()
                    .FontColor("#003366");

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    // Header
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
                        table.Cell().Text(scr.ToString()).AlignCenter();
                        table.Cell().Text(max.ToString()).AlignCenter();
                    }
                });
            });
        }
    }
}
