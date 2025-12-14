using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CAT.AID.Web.PDF
{
    public class ReportGenerator : BasePdfDocument
    {
        private readonly Assessment _assessment;
        private readonly AssessmentScoreDTO _score;
        private readonly List<AssessmentSection> _sections;

        public ReportGenerator(Assessment a, AssessmentScoreDTO score, List<AssessmentSection> sections)
        {
            _assessment = a;
            _score = score;
            _sections = sections;

            Title = "Comprehensive Vocational Assessment – Summary Report";
        }

        public override void ComposeBody(IContainer container)
        {
            var answers = string.IsNullOrWhiteSpace(_assessment.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(
                    _assessment.AssessmentResultJson
                ) ?? new Dictionary<string, string>();

            container.PaddingVertical(10).Column(col =>
            {
                // ---------------- Candidate Info ----------------
                col.Item().Element(e => SectionTitle(e, "Candidate Information"));
                col.Item().Element(e => CandidateBlock(e, _assessment.Candidate));

                // ---------------- Score Summary ----------------
                col.Item().Element(e => SectionTitle(e, "Score Summary"));
                col.Item().Element(e => ScoreSummary(e));

                // ---------------- Section Scores ----------------
                col.Item().Element(e => SectionTitle(e, "Section-Wise Scores"));
                col.Item().Element(e => SectionScoreTable(e));

                // ---------------- Question Breakdown (Optional) ----------------
                col.Item().Element(e => SectionTitle(e, "Key Questions Review"));
                col.Item().Element(e => QuestionBreakdown(e, answers));
            });
        }

        // ======================================================================
        // Candidate Information Block
        // ======================================================================
        private void CandidateBlock(IContainer container, Candidate c)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
            {
                // Photo
                row.ConstantItem(120).PaddingRight(10).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(c.PhotoFilePath))
                    {
                        var path = Path.Combine("wwwroot", c.PhotoFilePath.TrimStart('/'));
                        if (File.Exists(path))
                            col.Item().Image(path).FitArea();
                    }
                });

                // Details
                row.RelativeItem().Column(col =>
                {
                    Detail(col, "Name", c.FullName);
                    Detail(col, "Gender", c.Gender);
                    Detail(col, "DOB", c.DOB.ToString("dd-MMM-yyyy"));
                    Detail(col, "Disability Type", c.DisabilityType);
                    Detail(col, "Education", c.Education);
                    Detail(col, "Languages", c.OtherLanguages);
                    Detail(col, "Address", c.CommunicationAddress);
                    Detail(col, "Contact", c.ContactNumber);
                });
            });
        }

        private void Detail(ColumnDescriptor col, string label, string value)
        {
            col.Item().Text($"{label}: ").SemiBold().Span(value ?? "-");
        }

        // ======================================================================
        // Score Summary
        // ======================================================================
        private void ScoreSummary(IContainer container)
        {
            double pct = _score.MaxScore == 0 ? 0 : Math.Round((double)_score.TotalScore / _score.MaxScore * 100, 2);

            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
            .Column(col =>
            {
                col.Item().Text($"Total Score: {_score.TotalScore} / {_score.MaxScore}")
                    .FontSize(12).Bold();

                col.Item().Text($"Overall Performance: {pct}%")
                    .FontSize(12).FontColor(Colors.Blue.Medium);

                col.Item().Text($"Assessment Status: {_assessment.Status}")
                    .FontSize(12).SemiBold();
            });
        }

        // ======================================================================
        // Section Score Table
        // ======================================================================
        private void SectionScoreTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(1);
                });

                // Header
                table.Header(h =>
                {
                    h.Cell().Text("Section").SemiBold();
                    h.Cell().Text("Score").SemiBold();
                });

                foreach (var sec in _sections)
                {
                    int val = _score.SectionScores.ContainsKey(sec.Category)
                        ? _score.SectionScores[sec.Category]
                        : 0;

                    table.Cell().Text(sec.Category);
                    table.Cell().Text(val.ToString());
                }
            });
        }

        // ======================================================================
        // Question Breakdown (Short version)
        // ======================================================================
        private void QuestionBreakdown(IContainer container, Dictionary<string, string> ans)
        {
            container.PaddingTop(10).Column(col =>
            {
                foreach (var sec in _sections)
                {
                    col.Item().Text(sec.Category).Bold().FontSize(11);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Question").SemiBold();
                            h.Cell().Text("Score").SemiBold();
                        });

                        foreach (var q in sec.Questions)
                        {
                            ans.TryGetValue($"SCORE_{q.Id}", out var score);
                            score ??= "0";

                            t.Cell().Text(q.Text);
                            t.Cell().Text(score);
                        }
                    });

                    col.Item().PaddingBottom(10);
                }
            });
        }
    }
}
