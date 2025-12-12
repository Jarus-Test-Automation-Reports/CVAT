using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Models.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.Json;

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
            // Defensive: ensure non-null inputs
            a ??= new Assessment { Candidate = new Candidate { FullName = "Unknown" } };
            sections ??= new List<AssessmentSection>();
            recommendations ??= new Dictionary<string, List<string>>();
            score ??= new AssessmentScoreDTO();

            // Parse saved answers once
            Dictionary<string, string> answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new Dictionary<string, string>();

            using var ms = new MemoryStream();

            var doc = new Document(PageSize.A4, 40, 40, 40, 40);

            // Use PdfWriter within using so it gets disposed
            var writer = PdfWriter.GetInstance(doc, ms);
            try
            {
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

                string submitted = a.SubmittedAt.HasValue
                    ? a.SubmittedAt.Value.ToString("dd-MMM-yyyy")
                    : "—";
                doc.Add(new Paragraph($"{a.Candidate?.FullName} — {submitted}", textFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph("\n"));

                // ---------- SUMMARY ----------
                doc.Add(new Paragraph("📌 SUMMARY", sectionFont));
                doc.Add(new Paragraph($"Total Score: {score.TotalScore} / {score.MaxScore}", textFont));
                double iqPercent = score.MaxScore > 0
                    ? Math.Round((double)score.TotalScore / score.MaxScore * 100, 2)
                    : 0;
                doc.Add(new Paragraph($"IQ %: {iqPercent}%", textFont));
                doc.Add(new Paragraph($"Status: {a.Status}", textFont));
                doc.Add(new Paragraph("\n"));

                // ---------- RECOMMENDATIONS ----------
                doc.Add(new Paragraph("🎯 RECOMMENDATIONS", sectionFont));
                if (recommendations.Any())
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

                    // Header row (bold)
                    var hdrFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                    table.AddCell(new PdfPCell(new Phrase("Question", hdrFont)) { Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase("Score", hdrFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Comments", hdrFont)) { Padding = 6 });

                    foreach (var q in sec.Questions)
                    {
                        answers.TryGetValue($"ANS_{q.Id}", out string ans);
                        answers.TryGetValue($"SCORE_{q.Id}", out string scr);
                        answers.TryGetValue($"CMT_{q.Id}", out string cmt);

                        ans = string.IsNullOrWhiteSpace(ans) ? "-" : ans;
                        scr = string.IsNullOrWhiteSpace(scr) ? "0" : scr;
                        cmt = string.IsNullOrWhiteSpace(cmt) ? "-" : cmt;

                        table.AddCell(new PdfPCell(new Phrase(q.Text, textFont)) { Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(scr, textFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(cmt, textFont)) { Padding = 6 });
                    }

                    doc.Add(table);
                    doc.Add(new Paragraph("\n"));
                }

                // ---------- CHART IMAGES ----------
                // iTextSharp Image.GetInstance accepts byte[] directly
                if (barChart != null && barChart.Length > 0)
                {
                    try
                    {
                        var chart1 = iTextSharp.text.Image.GetInstance(barChart);
                        chart1.ScaleToFit(420f, 250f);
                        chart1.Alignment = Element.ALIGN_CENTER;
                        doc.Add(chart1);
                        doc.Add(new Paragraph("\n"));
                    }
                    catch
                    {
                        // ignore chart if invalid image bytes
                    }
                }

                if (doughnutChart != null && doughnutChart.Length > 0)
                {
                    try
                    {
                        var chart2 = iTextSharp.text.Image.GetInstance(doughnutChart);
                        chart2.ScaleToFit(300f, 200f);
                        chart2.Alignment = Element.ALIGN_CENTER;
                        doc.Add(chart2);
                        doc.Add(new Paragraph("\n"));
                    }
                    catch
                    {
                        // ignore invalid chart bytes
                    }
                }
            }
            finally
            {
                // ensure document & writer are closed
                if (doc.IsOpen()) doc.Close();
                writer?.Close();
            }

            return ms.ToArray();
        }
    }
}
