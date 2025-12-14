using CAT.AID.Models;
using CAT.AID.Models.DTO;
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
        // --------------------------------------------------------------------
        // SAFE DEFAULTS
        // --------------------------------------------------------------------
        a ??= new Assessment { Candidate = new Candidate { FullName = "Unknown" } };
        sections ??= new List<AssessmentSection>();
        score ??= new AssessmentScoreDTO();
        recommendations ??= new Dictionary<string, List<string>>();

        var answers =
            string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson)
                    ?? new Dictionary<string, string>();

        using var ms = new MemoryStream();

        // --------------------------------------------------------------------
        // DOCUMENT
        // --------------------------------------------------------------------
        var doc = new Document(PageSize.A4, 36, 36, 36, 36);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        // --------------------------------------------------------------------
        // UNICODE FONT (IMPORTANT FOR TELUGU / HINDI / TAMIL)
        // --------------------------------------------------------------------
        string fontPath = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";

        BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

        var titleFont = new Font(bf, 20, Font.BOLD);
        var headerFont = new Font(bf, 14, Font.BOLD, new BaseColor(0, 64, 140));
        var textFont = new Font(bf, 11, Font.NORMAL);
        var redFont = new Font(bf, 12, Font.BOLD, BaseColor.RED);
        var greenFont = new Font(bf, 12, Font.BOLD, BaseColor.GREEN);
        var bold = new Font(bf, 11, Font.BOLD);

        // --------------------------------------------------------------------
        // TITLE
        // --------------------------------------------------------------------
        var title = new Paragraph("ASSESSMENT REPORT", titleFont)
        {
            Alignment = Element.ALIGN_CENTER
        };
        doc.Add(title);

        doc.Add(new Paragraph(" ", textFont));

        // --------------------------------------------------------------------
        // CANDIDATE SUMMARY
        // --------------------------------------------------------------------
        string dateStr = a.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "—";

        doc.Add(new Paragraph($"Name: {a.Candidate.FullName}", textFont));
        doc.Add(new Paragraph($"DOB: {a.Candidate.DOB:dd-MMM-yyyy}", textFont));
        doc.Add(new Paragraph($"Gender: {a.Candidate.Gender}", textFont));
        doc.Add(new Paragraph($"Disability: {a.Candidate.DisabilityType}", textFont));
        doc.Add(new Paragraph($"Submitted: {dateStr}", textFont));
        doc.Add(new Paragraph($"Overall Score: {score.TotalScore} / {score.MaxScore}", bold));

        doc.Add(new Paragraph("\n"));

        // --------------------------------------------------------------------
        // RECOMMENDATIONS
        // --------------------------------------------------------------------
        doc.Add(new Paragraph("RECOMMENDATIONS", headerFont));

        if (recommendations.Any())
        {
            foreach (var sec in recommendations)
            {
                doc.Add(new Paragraph(sec.Key, redFont));

                var list = new iTextSharp.text.List(List.UNORDERED, 10f);
                foreach (var rec in sec.Value)
                    list.Add(new ListItem(rec, textFont));

                doc.Add(list);
            }
        }
        else
        {
            doc.Add(new Paragraph("No recommendations required.", greenFont));
        }

        doc.Add(new Paragraph("\n\n"));

        // --------------------------------------------------------------------
        // SECTION TABLES
        // --------------------------------------------------------------------
        doc.Add(new Paragraph("SECTION BREAKDOWN", headerFont));
        doc.Add(new Paragraph("\n"));

        foreach (var sec in sections)
        {
            doc.Add(new Paragraph(sec.Category, headerFont));

            PdfPTable table = new PdfPTable(3)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 60f, 10f, 30f });

            table.AddCell(new PdfPCell(new Phrase("Question", bold)) { Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("Score", bold)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Comments", bold)) { Padding = 6 });

            foreach (var q in sec.Questions)
            {
                answers.TryGetValue($"SCORE_{q.Id}", out string scr);
                answers.TryGetValue($"CMT_{q.Id}", out string cmt);

                table.AddCell(new PdfPCell(new Phrase(q.Text, textFont)) { Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase(scr ?? "0", textFont))
                {
                    Padding = 6,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                table.AddCell(new PdfPCell(new Phrase(
                    string.IsNullOrWhiteSpace(cmt) ? "-" : cmt, textFont))
                {
                    Padding = 6
                });
            }

            doc.Add(table);
            doc.Add(new Paragraph("\n"));
        }

        // --------------------------------------------------------------------
        // CHARTS
        // --------------------------------------------------------------------
        AddChart(doc, barChart, maxW: 420, maxH: 260);
        AddChart(doc, doughnutChart, maxW: 300, maxH: 200);

        // --------------------------------------------------------------------
        // CLOSE
        // --------------------------------------------------------------------
        doc.Close();
        writer.Close();

        return ms.ToArray();
    }

    // ------------------------------------------------------------------------
    // SAFE CHART IMAGE LOADER
    // ------------------------------------------------------------------------
    private static void AddChart(Document doc, byte[] data, float maxW, float maxH)
    {
        if (data == null || data.Length == 0) return;

        try
        {
            var img = Image.GetInstance(data);
            img.Alignment = Element.ALIGN_CENTER;
            img.ScaleToFit(maxW, maxH);
            doc.Add(img);
            doc.Add(new Paragraph("\n"));
        }
        catch { /* ignore invalid images */ }
    }
}
