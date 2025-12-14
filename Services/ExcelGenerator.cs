using CAT.AID.Models;
using CAT.AID.Models.DTO;
using OfficeOpenXml;
using System.Text.Json;

public static class ExcelGenerator
{
    public static byte[] BuildScoreSheet(Assessment a)
    {
        // ------------------------------------------------------
        // EPPlus license (required for .NET / Docker)
        // ------------------------------------------------------
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // ------------------------------------------------------
        // SAFELY PARSE SCORE JSON
        // ------------------------------------------------------
        AssessmentScoreDTO score;

        try
        {
            score = string.IsNullOrWhiteSpace(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)
                    ?? new AssessmentScoreDTO();
        }
        catch
        {
            score = new AssessmentScoreDTO();
        }

        score.SectionScores ??= new Dictionary<string, int>();

        // ------------------------------------------------------
        // CREATE EXCEL
        // ------------------------------------------------------
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Scores");

        // Header
        ws.Cells["A1"].Value = "Section";
        ws.Cells["B1"].Value = "Score";

        int row = 2;

        // Rows
        foreach (var s in score.SectionScores)
        {
            ws.Cells[row, 1].Value = s.Key;
            ws.Cells[row, 2].Value = s.Value;
            row++;
        }

        // Total at bottom
        ws.Cells[row + 1, 1].Value = "Total";
        ws.Cells[row + 1, 2].Value = score.TotalScore;

        // Auto-fit columns
        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        return pkg.GetAsByteArray();
    }
}
