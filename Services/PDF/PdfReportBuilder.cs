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
        // ------------------------------------------------------------
        // SAFE DEFAULTS
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // DOCUMENT
        // ------------------------------------------------------------
        var doc = new Document(PageSize.A4, 36, 36, 36, 36);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        // ------------------------------------------------------------
        // UNICODE FONT (Telugu / Tamil / Hindi / Kannada / etc.)
        // ------------------------------------------------------------
        string fontPath = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";

        BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

        var titleFont = new Font(bf, 20, Font.BOLD);
        var headerFont = new Font(bf, 14, Font.BOLD, new BaseColor(0, 64, 140));
        var textFont = new Font(bf, 11);
        var redFont = new Font(bf, 12, Font.BOLD, BaseColor.RED);
        var greenFont = new Font(bf, 12, Font.BOLD, BaseColor.GREEN);
        var bold = new Font(bf, 11, Font.BOLD);

        // ------------------------------------------------------------
        // TITLE
        // ------------------------------------------------------------
        doc.Add(new Paragraph("ASSESSMENT REPORT", titleFont)
        {
            Alignment = Element.ALIGN_CENTER
        });

        doc.Add(new Paragraph("\n", textFont));

        // ------------------------------------------------------------
        // CANDIDATE INFORMATION
        // ------------------------------------------------------------
        string dateStr = a.SubmittedAt?.ToString("dd-MMM-yyyy") ?? "—";

        doc.Add(new Paragraph($"Name: {a.Candidate.FullName}", textFont));
        doc.Add(new Paragraph($"DOB: {a.Candidate.DOB:dd-MMM-yyyy}", textFont));
        doc.Add(new Paragraph($"Gender: {a.Candidate.Gender}", textFont));
        doc.Add(new Paragraph($"Disability: {a.Candidate.DisabilityType}", textFont));
        doc.Add(new Paragraph($"Submitted: {dateStr}", textFont));
        doc.Add(new Paragraph($"Overall Score: {score.TotalScore} / {score.MaxScore}", bold));

        doc.Add(new Paragraph("\n"));

        // ------------------------------------------------------------
        // RECOMMENDATIONS
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // SECTION BREAKDOWN
        // ------------------------------------------------------------
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

            // Header
            table.AddCell(HeaderCell("Question", bold));
            table.AddCell(HeaderCell("Score", bold, Element.ALIGN_CENTER));
            table.AddCell(HeaderCell("Comments", bold));

            foreach (var q in sec.Questions)
            {
                answers.TryGetValue($"SCORE_{q.Id}", out string scoreVal);
                answers.TryGetValue($"CMT_{q.Id}", out string commentVal);

                table.AddCell(Cell(q.Text, textFont));
                table.AddCell(Cell(scoreVal ?? "0", textFont, Element.ALIGN_CENTER));
                table.AddCell(Cell(string.IsNullOrWhiteSpace(commentVal) ? "-" : commentVal, textFont));
            }

            doc.Add(table);
            doc.Add(new Paragraph("\n"));
        }

        // ------------------------------------------------------------
        // CHARTS
        // ------------------------------------------------------------
        AddChart(doc, barChart, 440, 260);
        AddChart(doc, doughnutChart, 300, 200);

        doc.Close();
        writer.Close();

        return ms.ToArray();
    }

    // ------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------
    private static PdfPCell HeaderCell(string text, Font font, int align = Element.ALIGN_LEFT)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            Padding = 6,
            BackgroundColor = new BaseColor(230, 230, 230),
            HorizontalAlignment = align
        };
    }

    private static PdfPCell Cell(string text, Font font, int align = Element.ALIGN_LEFT)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            Padding = 6,
            HorizontalAlignment = align
        };
    }

    private static void AddChart(Document doc, byte[] data, float maxW, float maxH)
    {
        if (data == null || data.Length == 0) return;

        try
        {
            var img = Image.GetInstance(data);
            img.ScaleToFit(maxW, maxH);
            img.Alignment = Element.ALIGN_CENTER;

            doc.Add(img);
            doc.Add(new Paragraph("\n"));
        }
        catch
        {
            // Skip corrupted chart images
        }
    }
}
