using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CAT.AID.Web.Services.Reports
{
    public static class ProgressReportGenerator
    {
        public static byte[] Build(Candidate c, List<Assessment> history)
            => GeneratePdf(c, history);

        public static byte[] GeneratePdf(Candidate c, List<Assessment> history)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header().AlignCenter().Text(text =>
                    {
                        text.Span("NATIONAL INSTITUTE FOR THE EMPOWERMENT OF PERSONS WITH INTELLECTUAL DISABILITIES\n")
                            .SemiBold().FontSize(12);
                        text.Span("Comprehensive Assessment Tool for Adults with Intellectual Disabilities (CAT-AID)")
                            .SemiBold().FontSize(11);
                    });

                    page.Content().PaddingVertical(15).Stack(stack =>
                    {
                        // Candidate box
                        stack.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Stack(s =>
                        {
                            s.Item().Text($"Candidate Name: {c.FullName}").FontSize(12);
                            s.Item().Text($"Location: {c.CommunicationAddress}").FontSize(12);
                        });

                        stack.Spacing(14);

                        // Progress Table
                        stack.Item().PaddingBottom(4).Text("Progress Summary").FontSize(14).Bold().Underline();

                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().PaddingBottom(4).Text("Assessment Date").Bold();
                                h.Cell().PaddingBottom(4).Text("Score").Bold();
                                h.Cell().PaddingBottom(4).Text("Percentage").Bold();
                            });

                            foreach (var a in history)
                            {
                                var score = string.IsNullOrEmpty(a.ScoreJson)
                                    ? new AssessmentScoreDTO()
                                    : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

                                table.Cell().Text(a.SubmittedAt?.ToString("dd-MMM-yyyy HH:mm") ?? "N/A");
                                table.Cell().Text($"{score.TotalScore} / {score.MaxScore}");
                                table.Cell().Text($"{score.Percentage:0.0}%");
                            }
                        });

                        stack.Spacing(14);

                        // Section-wise bar chart
                        stack.Item().PaddingBottom(4).Text("Section-wise Score Trend").FontSize(14).Bold().Underline();

                        foreach (var a in history)
                        {
                            var score = string.IsNullOrEmpty(a.ScoreJson)
                                ? new AssessmentScoreDTO()
                                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

                            stack.Item().PaddingTop(6).Text($"Assessment {a.Id}").Bold().FontSize(12);

                            foreach (var sec in score.SectionScores)
                            {
                                double percent = (score.MaxScore == 0)
                                    ? 0
                                    : (sec.Value / (score.MaxScore / score.SectionScores.Count)) * 100;

                                stack.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(sec.Key).FontSize(11);

                                    row.RelativeItem(2).Height(10).Border(1).BorderColor(Colors.Grey.Lighten2).Element(bar =>
                                    {
                                        double factor = percent / 100.0;     // percentage to fraction
                                        bar.Row(x =>
                                        {
                                            x.ConstantItem((float)(factor * 200)).Background(Colors.Blue.Medium); // filled portion
                                            x.RelativeItem().Background(Colors.White);                            // remaining portion
                                        });
                                    });


                                    row.RelativeItem().Text($"{percent:0.0}%").FontSize(11);
                                });
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(10);
                        x.CurrentPageNumber().FontSize(10);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
