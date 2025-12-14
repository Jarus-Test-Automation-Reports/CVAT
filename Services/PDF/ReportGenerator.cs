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
        private static readonly string LogoLeft =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "20240912282747915.png");

        private static readonly string LogoRight =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "202409121913074416.png");

        public static byte[] BuildAssessmentReport(Assessment a)
        {
            var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)
                    ?? new AssessmentScoreDTO();

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


    // ============================================================================
    //                           SIMPLE ASSESSMENT PDF
    // ============================================================================
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


        // ============================================================================
        public override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Element(CandidateSection);
                col.Item().Element(ScoreSummarySection);
                col.Item().Element(SectionScoresTable);
            });
        }


        // ============================================================================
        //                            CANDIDATE DETAILS
        // ============================================================================
        private void CandidateSection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Candidate Details");

                section.Content().Column(col =>
                {
                    col.Item().Text($"Name: {A.Candidate.FullName}");
                    col.Item().Text($"DOB: {A.Candidate.DOB:dd-MMM-yyyy}");
                    col.Item().Text($"Gender: {A.Candidate.Gender}");
                    col.Item().Text($"Disability: {A.Candidate.DisabilityType}");
                    col.Item().Text($"Address: {A.Candidate.CommunicationAddress}");

                    if (!string.IsNullOrWhiteSpace(A.Candidate.PhotoFilePath))
                    {
                        var absPath = Path.Combine(Directory.GetCurrentDirectory(), A.Candidate.PhotoFilePath);

                        if (File.Exists(absPath))
                            col.Item().AlignCenter().Image(absPath);
                    }
                });
            });
        }


        // ============================================================================
        //                            SCORE SUMMARY
        // ============================================================================
        private void ScoreSummarySection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Assessment Summary");

                double pct = Score.MaxScore > 0
                    ? (Score.TotalScore * 100.0 / Score.MaxScore)
                    : 0;

                section.Content().Column(col =>
                {
                    col.Item().Text($"Total Score: {Score.TotalScore}");
                    col.Item().Text($"Maximum Score: {Score.MaxScore}");
                    col.Item().Text($"Percentage: {pct:F1}%");
                    col.Item().Text($"Status: {A.Status}");
                    col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}");
                });
            });
        }


        // ============================================================================
        //                         SECTION SCORES TABLE
        // ============================================================================
        private void SectionScoresTable(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Section Scores");

                section.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
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
                        table.Cell().Text(scr.ToString());
                        table.Cell().Text(max.ToString());
                    }
                });
            });
        }
    }
}
