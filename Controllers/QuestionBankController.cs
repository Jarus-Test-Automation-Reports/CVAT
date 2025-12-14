using CAT.AID.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text.Json;

public class QuestionBankController : Controller
{
    private readonly IWebHostEnvironment _env;
    private string FilePath => Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");

    public QuestionBankController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index()
    {
        var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));
        return View(sections);
    }

    // -------- ADD / EDIT (GET) --------
    [HttpGet]
    public IActionResult Edit(int? id)
    {
        var all = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));

        AssessmentQuestion q = null;
        string section = "";
        if (id.HasValue)
        {
            q = all.SelectMany(s => s.Questions).FirstOrDefault(x => x.Id == id);
            section = all.FirstOrDefault(s => s.Questions.Any(x => x.Id == id))?.Category ?? "";
        }

        return View((section, q ?? new AssessmentQuestion(), all));
    }

    // -------- ADD / EDIT (POST) --------
    [HttpPost]
    public IActionResult Edit(string section, AssessmentQuestion q)
    {
        var all = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));

        var sec = all.FirstOrDefault(x => x.Category == section);
        if (sec == null) { sec = new AssessmentSection { Category = section, Questions = new() }; all.Add(sec); }

        var existing = sec.Questions.FirstOrDefault(x => x.Id == q.Id);
        if (existing == null)
        {
            q.Id = all.SelectMany(s => s.Questions).Any()
                 ? all.SelectMany(s => s.Questions).Max(x => x.Id) + 1
                 : 1;
            sec.Questions.Add(q);
        }
        else
        {
            existing.Text = q.Text;
            existing.Options = q.Options;
            existing.Correct = q.Correct;
            existing.ScoreWeight = q.ScoreWeight;
        }

        System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        TempData["msg"] = "✔ Question saved successfully!";
        return RedirectToAction(nameof(Index));
    }

    // -------- DELETE --------
    public IActionResult Delete(int id)
    {
        var all = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));
        foreach (var sec in all) sec.Questions.RemoveAll(x => x.Id == id);

        System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        TempData["msg"] = "🗑 Question deleted!";
        return RedirectToAction(nameof(Index));
    }

    // -------- DOWNLOAD EXCEL --------
    public IActionResult Download()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Questions");
        ws.Cells[1, 1].Value = "Section / Domain";
        ws.Cells[1, 2].Value = "Question ID";
        ws.Cells[1, 3].Value = "Question Text";
        ws.Cells[1, 4].Value = "Options";
        ws.Cells[1, 5].Value = "Correct Answer";
        ws.Cells[1, 6].Value = "Score Weight";

        int row = 2;
        foreach (var sec in sections)
            foreach (var q in sec.Questions)
            {
                ws.Cells[row, 1].Value = sec.Category;
                ws.Cells[row, 2].Value = q.Id;
                ws.Cells[row, 3].Value = q.Text;
                ws.Cells[row, 4].Value = string.Join(",", q.Options);
                ws.Cells[row, 5].Value = q.Correct;
                ws.Cells[row, 6].Value = q.ScoreWeight;
                row++;
            }

        return File(package.GetAsByteArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "QuestionBank.xlsx");
    }

    // -------- UPLOAD EXCEL --------
    [HttpPost]
    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || !file.FileName.EndsWith(".xlsx"))
        {
            TempData["error"] = "❌ Please upload a valid Excel (.xlsx) file.";
            return RedirectToAction(nameof(Index));
        }

        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        // Load existing JSON
        var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(FilePath));

        using var package = new ExcelPackage(file.OpenReadStream());
        var ws = package.Workbook.Worksheets[0];

        int row = 2;
        while (ws.Cells[row, 1].Value != null)
        {
            string section = ws.Cells[row, 1].Text.Trim();
            int.TryParse(ws.Cells[row, 2].Text, out int id);
            string text = ws.Cells[row, 3].Text.Trim();
            var options = ws.Cells[row, 4].Text.Split(',').Select(x => x.Trim()).ToList();
            string correct = ws.Cells[row, 5].Text.Trim();
            int.TryParse(ws.Cells[row, 6].Text, out int scoreWeight);

            // Find or create Section
            var secObj = sections.FirstOrDefault(s => s.Category == section);
            if (secObj == null)
            {
                secObj = new AssessmentSection { Category = section, Questions = new() };
                sections.Add(secObj);
            }

            // Find question by ID
            var q = secObj.Questions.FirstOrDefault(x => x.Id == id);

            if (q == null)
            {
                // Assign next ID if missing
                id = sections.SelectMany(x => x.Questions).Any()
                     ? sections.SelectMany(x => x.Questions).Max(x => x.Id) + 1
                     : 1;

                secObj.Questions.Add(new AssessmentQuestion
                {
                    Id = id,
                    Text = text,
                    Options = options,
                    Correct = correct,
                    ScoreWeight = scoreWeight
                });
            }
            else
            {
                q.Text = text;
                q.Options = options;
                q.Correct = correct;
                q.ScoreWeight = scoreWeight;
            }

            row++;
        }

        // 🔥 FINAL STEP: Save Back to JSON file
        var json = JsonSerializer.Serialize(sections, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(FilePath, json);

        TempData["msg"] = "✔ Excel import completed successfully and saved to JSON!";
        return RedirectToAction(nameof(Index));
    }

}

