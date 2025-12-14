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
            Score = score ?? new AssessmentScoreDTO();
            Sections = sections ?? new List<AssessmentSection>();
            Recommendations = recommendations ?? new Dictionary<string, List<string>>();
            BarChart = barChart;
            DoughnutChart = doughnutChart;

            Answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new Dictionary<string, string>();
        }

        // MAIN BODY
        public override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
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

        // ----------------------------------------------------------------------
        //                         COVER PAGE
        // ----------------------------------------------------------------------
        private void CoverPage(IContainer container)
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
                        var img = Path.Combine(Directory.GetCurrentDirectory(), A.Candidate.PhotoFilePath);
                        if (File.Exists(img))
                            col.Item().Image(img);
                    }
                });
            });
        }

        // ----------------------------------------------------------------------
        //                         SUMMARY SECTION
        // ----------------------------------------------------------------------
        private void SummarySection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Assessment Summary");

                double pct = Score.MaxScore > 0
                    ? Score.TotalScore * 100.0 / Score.MaxScore
                    : 0;

                section.Content().Column(col =>
                {
                    col.Item().Text($"Total Score: {Score.TotalScore} / {Score.MaxScore}");
                    col.Item().Text($"Percentage: {pct:F1}%");
                    col.Item().Text($"Status: {A.Status}");
                    col.Item().Text($"Submitted On: {A.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "--"}");

                    if (Answers.TryGetValue("SUMMARY_COMMENTS", out var summary))
                        col.Item().Text($"Comments:\n{summary}");
                });
            });
        }

        // ----------------------------------------------------------------------
        //                         RECOMMENDATIONS
        // ----------------------------------------------------------------------
        private void RecommendationsSection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Recommendations");

                section.Content().Column(col =>
                {
                    foreach (var group in Recommendations)
                    {
                        col.Item().Text(group.Key).Bold();

                        col.Item().List(list =>
                        {
                            foreach (var item in group.Value)
                                list.Item().Text(item);
                        });
                    }
                });
            });
        }

        // ----------------------------------------------------------------------
        //                         CHARTS
        // ----------------------------------------------------------------------
        private void ChartsSection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Assessment Charts");

                section.Content().Row(row =>
                {
                    if (BarChart?.Length > 0)
                        row.RelativeItem().Image(BarChart);

                    if (DoughnutChart?.Length > 0)
                        row.RelativeItem().Image(DoughnutChart);
                });
            });
        }

        // ----------------------------------------------------------------------
        //                         SECTION BREAKDOWN
        // ----------------------------------------------------------------------
        private void SectionBreakdown(IContainer container, AssessmentSection sec)
        {
            container.Section(section =>
            {
                section.Title(sec.Category);

                section.Content().Table(table =>
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
        //                         EVIDENCE LIST
        // ----------------------------------------------------------------------
        private void EvidenceSection(IContainer container)
        {
            var evidence = Answers
                .Where(x => x.Key.StartsWith("FILE_"))
                .Select(x => x.Value)
                .ToList();

            if (!evidence.Any())
                return;

            container.Section(section =>
            {
                section.Title("Evidence Files");

                section.Content().List(list =>
                {
                    foreach (var file in evidence)
                        list.Item().Text(file);
                });
            });
        }

        // ----------------------------------------------------------------------
        //                         SIGNATURES
        // ----------------------------------------------------------------------
        private void SignatureSection(IContainer container)
        {
            container.Section(section =>
            {
                section.Title("Signatures");

                section.Content().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Assessor").Bold();
                        c.Item().Text("______________________");
                        c.Item().Text(A.Assessor?.FullName ?? "-");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Lead Assessor").Bold();
                        c.Item().Text("______________________");
                        c.Item().Text(A.LeadAssessor?.FullName ?? "-");
                    });
                });
            });
        }
    }
}
