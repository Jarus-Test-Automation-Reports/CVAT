using CAT.AID.Models;
using CAT.AID.Models.DTO;
using OfficeOpenXml;
using System.Text.Json;

public static class ExcelGenerator
{
    public static byte[] BuildScoreSheet(Assessment a)
    {
        var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)!;

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Scores");

        ws.Cells["A1"].Value = "Section";
        ws.Cells["B1"].Value = "Score";

        int row = 2;
        foreach (var s in score.SectionScores)
        {
            ws.Cells[row, 1].Value = s.Key;
            ws.Cells[row, 2].Value = s.Value;
            row++;
        }

        ws.Cells[row + 1, 1].Value = "Total";
        ws.Cells[row + 1, 2].Value = score.TotalScore;

        return pkg.GetAsByteArray();
    }
}
