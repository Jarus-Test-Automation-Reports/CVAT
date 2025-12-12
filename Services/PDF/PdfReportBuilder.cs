using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Models.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

public static class PdfReportBuilder
{
    public static byte[] Build(Assessment a,
                               List<AssessmentSection> sections,
                               AssessmentScoreDTO score,
                               Dictionary<string, List<string>> recommendations,
                               byte[] barChart,
                               byte[] doughnutChart)
    {
        using var ms = new MemoryStream();
        var document = new Document(PageSize.A4, 36, 36, 36, 36);
        PdfWriter.GetInstance(document, ms);
        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
        var title = new Paragraph("Assessment Report", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        document.Add(title);

        // Candidate info
        var subFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
        document.Add(new Paragraph($"{a.Candidate.FullName} — {a.SubmittedAt:dd-MMM-yyyy}", subFont));
        document.Add(new Paragraph($"Total Score: {score.TotalScore} / {score.MaxScore}", subFont));
        document.Add(new Paragraph("\n"));

        // Recommendations
        var headFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        document.Add(new Paragraph("🎯 Recommendations", headFont));

        if (recommendations.Any())
        {
            foreach (var sec in recommendations)
            {
                var secFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.RED);
                document.Add(new Paragraph(sec.Key, secFont));

                var ul = new iTextSharp.text.List(List.UNORDERED);
                foreach (var rec in sec.Value)
                    ul.Add(new ListItem(rec));

                document.Add(ul);
            }
        }
        else
        {
            var green = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.GREEN);
            document.Add(new Paragraph("🌟 No recommendations required — all domains show strong performance.", green));
        }

        // Charts (optional)
        if (barChart.Length > 0)
        {
            var img = Image.GetInstance(barChart);
            img.ScaleToFit(400f, 250f);
            img.Alignment = Element.ALIGN_CENTER;
            document.Add(img);
        }

        if (doughnutChart.Length > 0)
        {
            var img2 = Image.GetInstance(doughnutChart);
            img2.ScaleToFit(300f, 200f);
            img2.Alignment = Element.ALIGN_CENTER;
            document.Add(img2);
        }

        document.Close();
        return ms.ToArray();
    }
}
