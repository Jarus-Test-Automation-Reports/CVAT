using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Models.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace CAT.AID.Web.Services.Pdf
{
    public class FullAssessmentPdfService
    {
        public byte[] Generate(
            Assessment a,
            AssessmentScoreDTO score,
            List<AssessmentSection> sections,
            Dictionary<string, List<string>> recommendations,
            byte[] barChart,
            byte[] doughnutChart)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // ---------- FONT STYLES ----------
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var redBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.RED);
                var greenBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.GREEN);
                var blueBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(0, 64, 140));

                // ---------- TITLE ----------
                doc.Add(new Paragraph("ASSESSMENT REPORT", titleFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph($"{a.Candidate.FullName} — {a.SubmittedAt?.ToString("dd-MMM-yyyy")}", textFont)
                { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph("\n"));

                // ---------- SUMMARY ----------
                doc.Add(new Paragraph("📌 SUMMARY", sectionFont));
                doc.Add(new Paragraph($"Total Score: {score.TotalScore} / {score.MaxScore}", textFont));
                double iqPercent = Math.Round((double)score.TotalScore / score.MaxScore * 100, 2);
                doc.Add(new Paragraph($"IQ %: {iqPercent}%", textFont));
                doc.Add(new Paragraph($"Status: {a.Status}", textFont));
                doc.Add(new Paragraph("\n"));

                // ---------- RECOMMENDATIONS ----------
                doc.Add(new Paragraph("🎯 RECOMMENDATIONS", sectionFont));
                if (recommendations != null && recommendations.Any())
                {
                    foreach (var sec in recommendations)
                    {
                        doc.Add(new Paragraph(sec.Key, redBold));

                        var ul = new List(List.UNORDERED);
                        foreach (var rec in sec.Value)
                            ul.Add(new ListItem(rec, textFont));

                        doc.Add(ul);
                    }
                }
                else
                {
                    doc.Add(new Paragraph("🌟 No recommendations required — all domains show strong performance.", greenBold));
                }

                doc.Add(new Paragraph("\n"));

                // ---------- SECTION QUESTION BREAKDOWN ----------
                doc.Add(new Paragraph("📑 SECTION BREAKDOWN", sectionFont));
                doc.Add(new Paragraph("\n"));

                foreach (var sec in sections)
                {
                    doc.Add(new Paragraph(sec.Category, blueBold));

                    PdfPTable table = new PdfPTable(3)
                    {
                        WidthPercentage = 100
                    };
                    table.SetWidths(new float[] { 60f, 10f, 30f });

                    table.AddCell(new Phrase("Question", textFont));
                    table.AddCell(new Phrase("Score", textFont));
                    table.AddCell(new Phrase("Comments", textFont));

                    foreach (var q in sec.Questions)
                    {
                        string ans = a.AssessmentResultJson != null
                            && System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                            .TryGetValue($"ANS_{q.Id}", out string value) ? value : "-";

                        string scr = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                            .TryGetValue($"SCORE_{q.Id}", out string scrValue) ? scrValue : "0";

                        string cmt = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                            .TryGetValue($"CMT_{q.Id}", out string cmtValue) ? cmtValue : "-";

                        table.AddCell(new Phrase(q.Text, textFont));
                        table.AddCell(new Phrase(scr, textFont));
                        table.AddCell(new Phrase(cmt, textFont));
                    }

                    doc.Add(table);
                    doc.Add(new Paragraph("\n"));
                }

                // ---------- CHART IMAGES ----------
                if (barChart.Length > 0)
                {
                    Image chart1 = Image.GetInstance(barChart);
                    chart1.ScaleToFit(420f, 250f);
                    chart1.Alignment = Element.ALIGN_CENTER;
                    doc.Add(chart1);
                }
                if (doughnutChart.Length > 0)
                {
                    Image chart2 = Image.GetInstance(doughnutChart);
                    chart2.ScaleToFit(300f, 200f);
                    chart2.Alignment = Element.ALIGN_CENTER;
                    doc.Add(chart2);
                }

                doc.Close();
                writer.Close();
                return ms.ToArray();
            }
        }
    }
}
