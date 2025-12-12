using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Data;
using CAT.AID.Web.Models;
using CAT.AID.Web.Models.DTO;
using CAT.AID.Web.Services;
using CAT.AID.Web.Services.Pdf;
using CAT.AID.Web.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class AssessmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IWebHostEnvironment _environment;

        public AssessmentsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> user,
            IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _environment = env;
        }

        // -------------------- 1. TASKS FOR ASSESSOR --------------------
        [Authorize(Roles = "LeadAssessor, Assessor")]
        public async Task<IActionResult> MyTasks()
        {
            var uid = _user.GetUserId(User)!;

            var tasks = await _db.Assessments
                .Include(a => a.Candidate)
                .Where(a => a.AssessorId == uid)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (!tasks.Any())
                return View(new List<CandidateAssessmentPivotVM>());

            var timestamps = tasks
                .Select(a => a.CreatedAt)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var grouped = tasks
                .GroupBy(a => a.CandidateId)
                .Select(g => new CandidateAssessmentPivotVM
                {
                    CandidateId = g.Key,
                    CandidateName = g.First().Candidate.FullName,

                    AssessmentIds = timestamps.ToDictionary(
                        ts => ts,
                        ts => g.FirstOrDefault(a => a.CreatedAt == ts)?.Id
                    ),

                    StatusMapping = g.ToDictionary(
                        a => a.Id,
                        a => a.Status.ToString()
                    )
                })
                .ToList();

            ViewBag.Timestamps = timestamps;
            return View(grouped);
        }

        // -------------------- COMPARE MULTIPLE ASSESSMENTS --------------------
        [Authorize(Roles = "Assessor, Lead, Admin")]
        [HttpGet]
        public async Task<IActionResult> Compare(int candidateId, int[] ids)
        {
            if (ids == null || ids.Length < 2)
                return BadRequest("At least two assessments must be selected.");

            var assessments = await _db.Assessments
                .Include(a => a.Candidate)
                .Where(a => ids.Contains(a.Id))
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();

            if (!assessments.Any())
                return NotFound();

            var scoreData = assessments.ToDictionary(
                a => a.Id,
                a => string.IsNullOrWhiteSpace(a.ScoreJson)
                    ? new AssessmentScoreDTO()
                    : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson)
            );

            ViewBag.Assessments = assessments;
            return View("CompareAssessments", scoreData);
        }

        // -------------------- 2. GET PERFORM ASSESSMENT --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> Perform(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var jsonFile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(jsonFile));
            ViewBag.Sections = sections;

            return View(a);
        }

        // -------------------- 3. SUBMIT ASSESSMENT --------------------
        [HttpPost]
        [Authorize(Roles = "Assessor, Lead")]
        public async Task<IActionResult> Perform(int id, string actionType)
        {
            var a = await _db.Assessments.FindAsync(id);
            if (a == null) return NotFound();
            if (!a.IsEditableByAssessor) return Unauthorized();

            var data = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
                if (key.StartsWith("ANS_") ||
                    key.StartsWith("SCORE_") ||
                    key.StartsWith("CMT_") ||
                    key == "SUMMARY_COMMENTS")
                    data[key] = Request.Form[key];

            // File uploads
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            foreach (var file in Request.Form.Files)
            {
                if (file.Length > 0)
                {
                    string name = $"{Guid.NewGuid()}_{file.FileName}";
                    string path = Path.Combine(uploadFolder, name);

                    using var stream = System.IO.File.Create(path);
                    await file.CopyToAsync(stream);

                    data[file.Name] = name;
                }
            }

            a.AssessmentResultJson = JsonSerializer.Serialize(data);

            // Build Score JSON
            var questionFile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sectionsData = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(questionFile));

            var scoreDto = new AssessmentScoreDTO();
            int totalMaxScore = 0;

            foreach (var sec in sectionsData)
            {
                int sectionTotal = 0;
                var questionScores = new Dictionary<string, int>();

                foreach (var q in sec.Questions)
                {
                    string key = $"SCORE_{q.Id}";
                    int maxPerQuestion = 3;
                    totalMaxScore += maxPerQuestion;

                    if (data.TryGetValue(key, out string val) && int.TryParse(val, out int scoreVal))
                    {
                        sectionTotal += scoreVal;
                        questionScores[q.Text] = scoreVal;
                    }
                }

                scoreDto.SectionScores[sec.Category] = sectionTotal;
                scoreDto.SectionQuestionScores[sec.Category] = questionScores;
            }

            scoreDto.TotalScore = scoreDto.SectionScores.Sum(x => x.Value);
            scoreDto.MaxScore = totalMaxScore;

            a.ScoreJson = JsonSerializer.Serialize(scoreDto);

            // Status logic
            if (actionType == "save")
            {
                a.Status = AssessmentStatus.InProgress;
                TempData["msg"] = "Assessment saved successfully!";
            }
            else if (actionType == "submit")
            {
                a.Status = AssessmentStatus.Submitted;
                a.SubmittedAt = DateTime.UtcNow;
                TempData["msg"] = "Assessment submitted for review!";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(MyTasks));
        }

        // -------------------- SUMMARY PAGE --------------------
        public IActionResult Summary(int id)
        {
            var a = _db.Assessments
                .Include(a => a.Candidate)
                .Include(a => a.Assessor)
                .FirstOrDefault(a => a.Id == id);

            if (a == null) return NotFound();

            var sections = _db.AssessmentSections.ToList();
            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

            var recFile = Path.Combine(_environment.WebRootPath, "data", "recommendations.json");
            var recommendationLibrary = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(System.IO.File.ReadAllText(recFile));

            var sectionMax = sections.ToDictionary(s => s.Category, s => s.Questions.Count * 3);

            var recommendations = new Dictionary<string, List<string>>();
            var weakDetails = new Dictionary<string, List<(string Question, int Score)>>();

            foreach (var sec in score.SectionScores)
            {
                double pct = (sec.Value / (double)sectionMax[sec.Key]) * 100;

                if (pct < 100 && recommendationLibrary.ContainsKey(sec.Key))
                    recommendations[sec.Key] = recommendationLibrary[sec.Key];

                var weakList = new List<(string Question, int Score)>();

                foreach (var q in sections.First(s => s.Category == sec.Key).Questions)
                {
                    var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson);
                    saved.TryGetValue($"SCORE_{q.Id}", out string scr);

                    int sc = int.TryParse(scr, out int x) ? x : 0;

                    if (sc < 3)
                        weakList.Add((q.Text, sc));
                }

                if (weakList.Any())
                    weakDetails[sec.Key] = weakList;
            }

            ViewBag.Recommendations = recommendations;
            ViewBag.WeakDetails = weakDetails;
            ViewBag.Sections = sections;

            return View(a);
        }

        // -------------------- EXPORT PDF (OLD REPORT) --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var pdf = ReportGenerator.BuildAssessmentReport(a);
            return File(pdf, "application/pdf", $"Assessment_{a.Id}.pdf");
        }

        // -------------------- EXPORT EXCEL --------------------
        [Authorize]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var file = ExcelGenerator.BuildScoreSheet(a);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Scores_{a.Id}.xlsx");
        }

        // -------------------- VIEW ASSESSMENT --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> View(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .Include(x => x.Assessor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson);

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;

            return View("ViewAssessment", a);
        }

        // -------------------- RECOMMENDATION VIEW --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> Recommendations(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

            var mapping = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                System.IO.File.ReadAllText(Path.Combine(_environment.WebRootPath, "data", "recommendations.json"))
            );

            var result = new Dictionary<string, List<string>>();

            foreach (var sec in score.SectionScores)
            {
                double pct = (sec.Value / (score.MaxScore / score.SectionScores.Count)) * 100;
                if (pct < 60)
                    result[sec.Key] = mapping[sec.Key];
            }

            ViewBag.Score = score;
            return View(result);
        }

        // -------------------- 6. REVIEW PAGE --------------------
        [Authorize(Roles = "LeadAssessor, Admin")]
        public async Task<IActionResult> Review(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .Include(x => x.Assessor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var answers = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson);

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;

            ViewBag.Assessors = await _db.Users
                .Where(u => u.Location == a.Candidate.CommunicationAddress)
                .ToListAsync();

            return View(a);
        }

        // -------------------- EXPORT FULL REPORT PDF --------------------
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ExportReportPdf(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .Include(x => x.Assessor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var score = string.IsNullOrWhiteSpace(a.ScoreJson)
                ? new AssessmentScoreDTO()
                : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(qfile));

            var recFile = Path.Combine(_environment.WebRootPath, "data", "recommendations.json");
            var recommendations = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                System.IO.File.ReadAllText(recFile));

            string barChartRaw = Request.Form["barChartImage"];
            string doughnutChartRaw = Request.Form["doughnutChartImage"];

            byte[] barChart = string.IsNullOrWhiteSpace(barChartRaw) ? Array.Empty<byte>() :
                Convert.FromBase64String(barChartRaw.Split(',')[1]);

            byte[] doughnutChart = string.IsNullOrWhiteSpace(doughnutChartRaw) ? Array.Empty<byte>() :
                Convert.FromBase64String(doughnutChartRaw.Split(',')[1]);

            var pdf = new FullAssessmentPdfService()
                .Generate(a, score, sections, recommendations, barChart, doughnutChart);

            return File(pdf, "application/pdf", $"Assessment_{a.Id}.pdf");
        }

        // -------------------- POST REVIEW ACTION --------------------
        [Authorize(Roles = "LeadAssessor, Admin")]
        [HttpPost]
        public async Task<IActionResult> Review(int id, string leadComments, string action, string? newAssessorId)
        {
            var a = await _db.Assessments.FindAsync(id);
            if (a == null) return NotFound();

            a.LeadComments = leadComments;
            a.ReviewedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(newAssessorId))
            {
                a.AssessorId = newAssessorId;
                a.Status = AssessmentStatus.Assigned;
            }

            if (action == "approve") a.Status = AssessmentStatus.Approved;
            if (action == "reject") a.Status = AssessmentStatus.Rejected;
            if (action == "sendback") a.Status = AssessmentStatus.SentBack;

            if (action == "lead-edit")
            {
                a.Status = AssessmentStatus.InProgress;
                a.AssessorId = _user.GetUserId(User)!;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("ReviewQueue");
        }

        // -------------------- REVIEW QUEUE --------------------
        [Authorize(Roles = "LeadAssessor, Admin")]
        public async Task<IActionResult> ReviewQueue()
        {
            var list = await _db.Assessments
                .Include(a => a.Candidate)
                .Where(a => a.Status == AssessmentStatus.Submitted)
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return View(list);
        }

        // -------------------- UPDATE STATUS --------------------
        [Authorize(Roles = "LeadAssessor, Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string action)
        {
            var a = await _db.Assessments.FindAsync(id);
            if (a == null) return NotFound();

            if (action == "approve") a.Status = AssessmentStatus.Approved;
            else if (action == "reject") a.Status = AssessmentStatus.Rejected;
            else if (action == "sendback") a.Status = AssessmentStatus.SentBack;

            a.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["msg"] = $"Assessment {action} successful!";
            return RedirectToAction(nameof(ReviewQueue));
        }

        // -------------------- 8. HISTORY --------------------
        [Authorize(Roles = "LeadAssessor, Admin")]
        public async Task<IActionResult> History(int candidateId)
        {
            var list = await _db.Assessments
                .Include(a => a.Candidate)
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return View(list);
        }
    }
}
