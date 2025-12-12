using CAT.AID.Models;
using CAT.AID.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CAT.AID.Web.Services.Reports
{
    public static class ReportGenerator
    {
        public static byte[] BuildAssessmentReport(Assessment a)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var candidate = a.Candidate!;
            var score = string.IsNullOrEmpty(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)!;

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);

                    // ---------- HEADER ----------
                    page.Header().Row(row =>
                    {
                        row.RelativeColumn().Text("Comprehensive Assessment Report")
                            .FontSize(20).Bold();

                        if (File.Exists(logoPath))
                            row.ConstantColumn(80).Image(logoPath);
                    });

                    // ---------- BODY ----------
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text($"Candidate Name: {candidate.FullName}").FontSize(14);
                      //  col.Item().Text($"Age: {candidate.Age}");
                        col.Item().Text($"Location: {candidate.CommunicationAddress}");
                        col.Item().Text($"Assessor Comments: {a.AssessorComments}");
                        col.Item().Text($"Lead Comments: {a.LeadComments}");

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(8).Text("Score Summary")
                            .FontSize(16).Bold();

                        col.Item().Text($"{score.TotalScore} / {score.MaxScore}")
                            .FontSize(14).Bold();

                        // ---------- TABLE ----------
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("Section").Bold();
                                h.Cell().Text("Score").Bold();
                            });

                            foreach (var s in score.SectionScores)
                            {
                                t.Cell().Text(s.Key);
                                t.Cell().Text(s.Value.ToString("0.0"));
                            }
                        });
                    });

                    // ---------- FOOTER ----------
                    page.Footer().AlignRight()
                        .Text($"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}");
                });
            });

            return doc.GeneratePdf();
        }
    }
}
