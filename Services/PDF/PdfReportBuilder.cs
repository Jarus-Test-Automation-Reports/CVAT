using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Models.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.Json;

public static class PdfReportBuilder
{
    public static byte[] Build(
        Assessment a,
        List<AssessmentSection> sections,
        AssessmentScoreDTO score,
        Dictionary<string, List<string>> recommendations,
        byte[] barChart,
        byte[] doughnutChart)
    {
        // Defensive defaults
        a ??= new Assessment { Candidate = new Candidate { FullName = "Unknown" } };
        sections ??= new List<AssessmentSection>();
        score ??= new AssessmentScoreDTO();
        recommendations ??= new Dictionary<string, List<string>>();

        // Parse saved assessor answers once
        Dictionary<string, string> answers =
            string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new Dictionary<string, string>();

        using var ms = new MemoryStream();
        var doc = new Document(PageSize.A4, 36, 36, 36, 36);

        using (var writer = PdfWriter.GetInstance(doc, ms))
        {
            doc.Open();

            // ---------------- FONTS ----------------
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var redFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.RED);
            var greenFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.GREEN);
            var secFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, new BaseColor(0, 64, 140));

            // ---------------- TITLE ----------------
            var title = new Paragraph("ASSESSMENT REPORT", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
            doc.Add(title);

            string dateStr = a.SubmittedAt.HasValue ? a.SubmittedAt.Value.ToString("dd-MMM-yyyy") : "—";
            doc.Add(new Paragraph($"{a.Candidate.FullName} — {dateStr}", textFont));
            doc.Add(new Paragraph($"Total Score: {score.TotalScore} / {score.MaxScore}", textFont));
            doc.Add(new Paragraph("\n"));


            // ---------------- RECOMMENDATIONS ----------------
            doc.Add(new Paragraph("🎯 RECOMMENDATIONS", headerFont));

            if (recommendations.Any())
            {
                foreach (var sec in recommendations)
                {
                    doc.Add(new Paragraph(sec.Key, redFont));

                    var list = new iTextSharp.text.List(List.UNORDERED);
                    foreach (var rec in sec.Value)
                        list.Add(new ListItem(rec, textFont));

                    doc.Add(list);
                }
            }
            else
            {
                doc.Add(new Paragraph(
                    "🌟 No recommendations required — strong performance across all domains.",
                    greenFont));
            }

            doc.Add(new Paragraph("\n"));


            // ---------------- SECTION SCORE TABLES ----------------
            doc.Add(new Paragraph("📑 SECTION BREAKDOWN", headerFont));
            doc.Add(new Paragraph("\n"));

            foreach (var sec in sections)
            {
                doc.Add(new Paragraph(sec.Category, secFont));

                PdfPTable table = new PdfPTable(3)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 60f, 10f, 30f });

                // Header row
                var bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                table.AddCell(new PdfPCell(new Phrase("Question", bold)) { Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Score", bold)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Comments", bold)) { Padding = 6 });

                foreach (var q in sec.Questions)
                {
                    answers.TryGetValue($"SCORE_{q.Id}", out string scr);
                    answers.TryGetValue($"CMT_{q.Id}", out string cmt);

                    scr = scr ?? "0";
                    cmt = string.IsNullOrWhiteSpace(cmt) ? "-" : cmt;

                    table.AddCell(new PdfPCell(new Phrase(q.Text, textFont)) { Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase(scr, textFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase(cmt, textFont)) { Padding = 6 });
                }

                doc.Add(table);
                doc.Add(new Paragraph("\n"));
            }


            // ---------------- CHARTS ----------------
            if (barChart != null && barChart.Length > 0)
            {
                try
                {
                    var img = Image.GetInstance(barChart);
                    img.ScaleToFit(420f, 260f);
                    img.Alignment = Element.ALIGN_CENTER;
                    doc.Add(img);
                }
                catch { /* ignore invalid image */ }
            }

            if (doughnutChart != null && doughnutChart.Length > 0)
            {
                try
                {
                    var img2 = Image.GetInstance(doughnutChart);
                    img2.ScaleToFit(300f, 200f);
                    img2.Alignment = Element.ALIGN_CENTER;
                    doc.Add(img2);
                }
                catch { }
            }

            doc.Close();
        }

        return ms.ToArray();
    }
}
