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
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;



namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class AssessmentsController : Controller
    {
        private readonly Dictionary<string, List<string>> _recommendationLibrary;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IWebHostEnvironment _environment;

        public AssessmentsController(ApplicationDbContext db, UserManager<ApplicationUser> user, IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _environment = env;
        }

        // -------------------- 1. TASKS FOR ASSESSOR --------------------
        [Authorize(Roles = "LeadAssessor, Assessor")]
        [Authorize(Roles = "Assessor")]
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

            // ⬇ FIX — Use full timestamp (NOT Date)
            var timestamps = tasks
                .Select(a => a.CreatedAt) // FULL datetime
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var grouped = tasks
                .GroupBy(a => a.CandidateId)
                .Select(g => new CandidateAssessmentPivotVM
                {
                    CandidateId = g.Key,
                    CandidateName = g.First().Candidate.FullName,

                    // Map each timestamp to assessment ID
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

            // Deserialize stored score JSON
            var scoreData = assessments
                .ToDictionary(a => a.Id, a =>
                    string.IsNullOrWhiteSpace(a.ScoreJson)
                    ? new AssessmentScoreDTO()
                    : System.Text.Json.JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson));

            ViewBag.Assessments = assessments;
            return View("CompareAssessments", scoreData);
        }

        // -------------------- 2. GET PERFORM ASSESSMENT --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> Perform(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var jsonPath = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(jsonPath));
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

            // 1️⃣ READ ALL FORM FIELDS (ANS_, SCORE_, CMT_, SUMMARY_COMMENTS)
            var data = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
                if (key.StartsWith("ANS_") || key.StartsWith("SCORE_") || key.StartsWith("CMT_") || key == "SUMMARY_COMMENTS")
                    data[key] = Request.Form[key];

            // 2️⃣ Handle FILE Uploads (optional)
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

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

            // 3️⃣ Save Answer JSON
            a.AssessmentResultJson = JsonSerializer.Serialize(data);

            // 4️⃣ Build SCORE JSON (100% corrected MaxScore)
            var sectionsData = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json"))
            );

            var scoreDto = new AssessmentScoreDTO();
            int totalMaxScore = 0;   // holds Q count × max score

            foreach (var sec in sectionsData)
            {
                int sectionTotal = 0;
                var questionScores = new Dictionary<string, int>();

                foreach (var q in sec.Questions)
                {
                    string scoreKey = $"SCORE_{q.Id}";
                    int maxPerQuestion = 3;     // change easily later
                    totalMaxScore += maxPerQuestion;

                    if (data.TryGetValue(scoreKey, out string scr) && int.TryParse(scr, out int scoreVal))
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

            // 5️⃣ Change Status Based On Action
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

            // 6️⃣ FINAL — Save to DB
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(MyTasks));
        }
        public IActionResult Summary(int id)
        {
            var a = _db.Assessments
                .Include(a => a.Candidate)
                .Include(a => a.Assessor)
                .FirstOrDefault(a => a.Id == id);

            if (a == null) return NotFound();

            // Load Sections from DB
            var sections = _db.AssessmentSections.ToList();

            // Load Score JSON
            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

            // Load Recommendation Library
            var recFile = Path.Combine(_environment.WebRootPath, "data", "recommendations.json");
            var recommendationLibrary = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                System.IO.File.ReadAllText(recFile)
            );

            // Build MAX score table (each question max = 3)
            var sectionMaxScores = sections.ToDictionary(
                s => s.Category,
                s => s.Questions.Count * 3
            );

            // ---------------- Build Recommendation List ----------------
            Dictionary<string, List<string>> recommendations = new();
            Dictionary<string, List<(string Question, int Score)>> weakDetails = new();

            foreach (var sec in score.SectionScores)
            {
                double max = sectionMaxScores[sec.Key];
                double pct = (sec.Value / max) * 100;

                // Show recommendations only if performance < 100%
                if (pct < 100 && recommendationLibrary.ContainsKey(sec.Key))
                {
                    recommendations[sec.Key] = recommendationLibrary[sec.Key];
                }

                // Weak question breakdown
                var weakList = new List<(string Question, int Score)>();
                foreach (var q in sections.First(s => s.Category == sec.Key).Questions)
                {
                    var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson);
                    saved.TryGetValue($"SCORE_{q.Id}", out string scr);
                    int sc = int.TryParse(scr, out int x) ? x : 0;

                    if (sc < 3)  // <3 means not fully achieved
                        weakList.Add((q.Text, sc));
                }

                if (weakList.Any())
                    weakDetails[sec.Key] = weakList;
            }

            // Send to UI
            ViewBag.Recommendations = recommendations;
            ViewBag.WeakDetails = weakDetails;
            ViewBag.Sections = sections;

            return View(a);
        }

        // -------------------- 4. SUMMARY RESULT DISPLAY --------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]

        [Authorize]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            var pdf = ReportGenerator.BuildAssessmentReport(a);
            return File(pdf, "application/pdf", $"Assessment_{a.Id}.pdf");
        }

        [Authorize]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var a = await _db.Assessments.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            var file = ExcelGenerator.BuildScoreSheet(a);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Scores_{a.Id}.xlsx");
        }



        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> View(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .Include(x => x.Assessor)   // 🔥 mandatory
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var answers = string.IsNullOrWhiteSpace(a.AssessmentDataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentDataJson);

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;
            ViewBag.RecommendationFile = Path.Combine(_environment.WebRootPath, "data", "recommendations.json");

            return View("ViewAssessment", a);
        }


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
                if (pct < 60)  // Low area
                    result[sec.Key] = mapping[sec.Key];
            }

            ViewBag.Score = score;
            return View(result);
        }


        // -------------------- 6. GET REVIEW --------------------

        [Authorize(Roles = "LeadAssessor, Admin")]
        public async Task<IActionResult> Review(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .Include(x => x.Assessor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            // load saved answers of assessor
            Dictionary<string, string> answers =
            string.IsNullOrWhiteSpace(a.AssessmentDataJson)
            ? new()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentDataJson);


            // load questions
            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;

            // assessor reassign dropdown
            ViewBag.Assessors = await _db.Users
                .Where(u => u.Location == a.Candidate.CommunicationAddress)
                .ToListAsync();

            return View(a);
        }
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
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            var recFile = Path.Combine(_environment.WebRootPath, "data", "recommendations.json");
            var recommendations = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(System.IO.File.ReadAllText(recFile));

            // Capture charts generated from browser (POSTed from hidden inputs)
            string barChartRaw = Request.Form["barChartImage"];
            string doughnutChartRaw = Request.Form["doughnutChartImage"];

            byte[] barChart = Array.Empty<byte>();
            byte[] doughnutChart = Array.Empty<byte>();

            if (!string.IsNullOrWhiteSpace(barChartRaw) && barChartRaw.Contains(","))
            {
                barChartRaw = barChartRaw.Split(',')[1];   // remove mime header
                barChart = Convert.FromBase64String(barChartRaw);
            }

            if (!string.IsNullOrWhiteSpace(doughnutChartRaw) && doughnutChartRaw.Contains(","))
            {
                doughnutChartRaw = doughnutChartRaw.Split(',')[1];
                doughnutChart = Convert.FromBase64String(doughnutChartRaw);
            }

            var pdf = new FullAssessmentPdfService()
                .Generate(a, score, sections, recommendations, barChart, doughnutChart);

            return File(pdf, "application/pdf", $"Assessment_{a.Id}.pdf");
        }


        // -------------------- 7. POST REVIEW ACTION --------------------


        [HttpPost]
        [Authorize(Roles = "LeadAssessor, Admin")]
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


        [Authorize(Roles = "LeadAssessor, Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string action)
        {
            var a = await _db.Assessments.FindAsync(id);
            if (a == null) return NotFound();

            if (action == "approve")
                a.Status = AssessmentStatus.Approved;

            else if (action == "reject")
                a.Status = AssessmentStatus.Rejected;

            else if (action == "sendback")
                a.Status = AssessmentStatus.SentBack;

            a.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["msg"] = $"Assessment {action} successful!";
            return RedirectToAction(nameof(ReviewQueue));
        }


        [Authorize(Roles = "LeadAssessor, Admin")]

        // -------------------- 8. HISTORY --------------------N
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
