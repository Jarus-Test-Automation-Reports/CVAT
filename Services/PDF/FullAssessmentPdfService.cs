using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Text.Json;

namespace CAT.AID.Web.PDF
{
    public class FullAssessmentPdfService : BasePdfDocument
    {
        private readonly Assessment _assessment;
        private readonly AssessmentScoreDTO _score;
        private readonly List<AssessmentSection> _sections;
        private readonly Dictionary<string, List<string>> _recommendations;

        private readonly byte[]? _barChart;
        private readonly byte[]? _doughnutChart;

        public FullAssessmentPdfService(
            Assessment assessment,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            Dictionary<string, List<string>> recommendations,
            byte[]? barChart,
            byte[]? doughnutChart)
        {
            _assessment = assessment;
            _score = score;
            _sections = sections ?? new();
            _recommendations = recommendations ?? new();

            _barChart = barChart;
            _doughnutChart = doughnutChart;

            Title = "Comprehensive Vocational Assessment Report";
        }

        // ============================================================
        // BODY
        // ============================================================
        public override void ComposeBody(IContainer container)
        {
            var ans = string.IsNullOrWhiteSpace(_assessment.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(
                    _assessment.AssessmentResultJson
                ) ?? new Dictionary<string, string>();

            container.PaddingVertical(10).Column(col =>
            {
                // ---------------- CANDIDATE DETAILS ----------------
                col.Item().Element(x => SectionTitle(x, "Candidate Information"));
                col.Item().Element(x => CandidateInfo(x, _assessment.Candidate));

                // ---------------- SCORE SUMMARY ----------------
                col.Item().Element(x => SectionTitle(x, "Assessment Summary"));
                col.Item().Element(x => SummaryBlock(x));

                // ---------------- RECOMMENDATIONS ----------------
                col.Item().Element(x => SectionTitle(x, "Recommendations"));
                col.Item().Element(x => RecommendationBlock(x));

                // ---------------- BREAKDOWN ----------------
                col.Item().Element(x => SectionTitle(x, "Section-Wise Breakdown"));
                col.Item().Element(x => BreakdownTable(x, ans));

                // ---------------- CHARTS ----------------
                col.Item().Element(x => Charts(x));

                // ---------------- SIGNATURES ----------------
                col.Item().Element(x => SectionTitle(x, "Signatures"));
                col.Item().Element(x => SignatureBlock(
                    x,
                    _assessment.Assessor?.FullName ?? "Assessor",
                    "Lead Assessor"
                ));
            });
        }

        // ============================================================
        // CANDIDATE INFO BLOCK
        // ============================================================
        private void CandidateInfo(IContainer container, Candidate c)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
            {
                // PHOTO
                row.ConstantItem(120).PaddingRight(10).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(c.PhotoFilePath))
                    {
                        var path = Path.Combine("wwwroot", c.PhotoFilePath.TrimStart('/'));
                        if (File.Exists(path))
                            col.Item().Image(path).FitArea();
                    }
                    else
                    {
                        col.Item().Text("No Photo").FontSize(10).Italic();
                    }
                });

                // DETAILS TABLE
                row.RelativeItem().Column(col =>
                {
                    Detail(col, "Name", c.FullName);
                    Detail(col, "Gender", c.Gender);
                    Detail(col, "DOB", c.DOB.ToString("dd-MMM-yyyy"));

                    Detail(col, "Disability Type", c.DisabilityType);
                    Detail(col, "Mother Tongue", c.MotherTongue);
                    Detail(col, "Education", c.Education);

                    Detail(col, "Contact", c.ContactNumber);
                    Detail(col, "Address", c.CommunicationAddress);
                });
            });
        }

        private void Detail(ColumnDescriptor col, string label, string value)
        {
            col.Item().Text($"{label}: ").SemiBold().Span(value ?? "-");
        }

        // ============================================================
        // SUMMARY BLOCK
        // ============================================================
        private void SummaryBlock(IContainer container)
        {
            double pct = _score.MaxScore > 0
                ? Math.Round((double)_score.TotalScore / _score.MaxScore * 100, 2)
                : 0;

            container.Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                .Column(col =>
                {
                    col.Item().Text($"Total Score: {_score.TotalScore} / {_score.MaxScore}")
                        .FontSize(12).Bold();

                    col.Item().Text($"Performance Percentage: {pct}%")
                        .FontSize(12).FontColor(Colors.Blue.Medium);

                    col.Item().Text($"Status: {_assessment.Status}")
                        .FontSize(12).Bold();
                });
        }

        // ============================================================
        // RECOMMENDATIONS
        // ============================================================
        private void RecommendationBlock(IContainer container)
        {
            container.Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                .Column(col =>
                {
                    if (_recommendations.Any())
                    {
                        foreach (var sec in _recommendations)
                        {
                            col.Item().Text(sec.Key).Bold().FontSize(12);

                            col.Item().Column(list =>
                            {
                                foreach (var r in sec.Value)
                                    list.Item().Text("• " + r);
                            });
                        }
                    }
                    else
                    {
                        col.Item().Text("No recommendations — all domains appear sufficient.")
                            .Italic();
                    }
                });
        }

        // ============================================================
        // BREAKDOWN BY QUESTION
        // ============================================================
        private void BreakdownTable(IContainer container, Dictionary<string, string> ans)
        {
            foreach (var section in _sections)
            {
                container.PaddingBottom(15).Column(col =>
                {
                    col.Item().Text(section.Category).Bold().FontSize(12);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(3);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Question").SemiBold();
                            h.Cell().Text("Score").SemiBold();
                            h.Cell().Text("Comments").SemiBold();
                        });

                        foreach (var q in section.Questions)
                        {
                            ans.TryGetValue($"SCORE_{q.Id}", out var scr);
                            ans.TryGetValue($"CMT_{q.Id}", out var cmt);

                            scr ??= "0";
                            cmt ??= "-";

                            t.Cell().Text(q.Text);
                            t.Cell().Text(scr);
                            t.Cell().Text(cmt);
                        }
                    });
                });
            }
        }

        // ============================================================
        // CHARTS
        // ============================================================
        private void Charts(IContainer container)
        {
            container.PaddingVertical(10).Column(col =>
            {
                if (_barChart != null && _barChart.Length > 0)
                {
                    col.Item().Image(_barChart);
                }

                if (_doughnutChart != null && _doughnutChart.Length > 0)
                {
                    col.Item().PaddingTop(10).Image(_doughnutChart);
                }
            });
        }
    }
}
