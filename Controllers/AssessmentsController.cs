using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Data;
using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Services.PDF;   // <-- ADD THIS

namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class AssessmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IWebHostEnvironment _env;

        public AssessmentsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> user,
            IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _env = env;
        }

        // ---------------------------------------------------------
        // 1. MY TASKS
        // ---------------------------------------------------------
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
                        a => a.Id, a => a.Status.ToString()
                    )
                })
                .ToList();

            ViewBag.Timestamps = timestamps;
            return View(grouped);
        }

        // ---------------------------------------------------------
        // 2. LOAD PERFORM PAGE
        // ---------------------------------------------------------
        [Authorize(Roles = "Assessor, LeadAssessor, Admin")]
        public async Task<IActionResult> Perform(int id)
        {
            var a = await _db.Assessments
                .Include(x => x.Candidate)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null)
                return NotFound();

            var jsonFile = Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(jsonFile));

            ViewBag.Sections = sections;
            return View(a);
        }

        // ---------------------------------------------------------
        // 3. SUBMIT / SAVE ASSESSMENT
        // ---------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "Assessor, LeadAssessor")]
        public async Task<IActionResult> Perform(int id, string actionType)
        {
            var a = await _db.Assessments.FindAsync(id);
            if (a == null) return NotFound();
            if (!a.IsEditableByAssessor) return Unauthorized();

            var data = new Dictionary<string, string>();

            // ---------------------------
            // Collect answers & comments
            // ---------------------------
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("ANS_") ||
                    key.StartsWith("SCORE_") ||
                    key.StartsWith("CMT_") ||
                    key == "SUMMARY_COMMENTS")
                {
                    data[key] = Request.Form[key];
                }
            }

            // ---------------------------
            // Multi-file uploads handling
            // ---------------------------
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            foreach (var file in Request.Form.Files)
            {
                if (!file.Name.StartsWith("FILE_UPLOAD_")) continue;
                if (file.Length == 0) continue;

                // Extract Question ID
                string qId = file.Name.Replace("FILE_UPLOAD_", "");

                int index = 0;
                while (data.ContainsKey($"FILE_{qId}_{index}")) index++;

                string storedName = $"{Guid.NewGuid()}_{file.FileName}";
                string storedPath = Path.Combine(uploadFolder, storedName);

                using var stream = System.IO.File.Create(storedPath);
                await file.CopyToAsync(stream);

                data[$"FILE_{qId}_{index}"] = storedName;
            }

            // ---------------------------
            // Save JSON in DB
            // ---------------------------
            a.AssessmentResultJson = JsonSerializer.Serialize(data);

            // ---------------------------
            // SCORE CALCULATION
            // ---------------------------
            string qFile = Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(qFile));

            var scoreDto = new AssessmentScoreDTO();

            foreach (var sec in sections)
            {
                int sectionScore = 0;
                var questionScoreMap = new Dictionary<string, int>();

                foreach (var q in sec.Questions)
                {
                    string key = $"SCORE_{q.Id}";
                    int maxScore = 3;

                    if (data.TryGetValue(key, out string val) && int.TryParse(val, out int sc))
                    {
                        sectionScore += sc;
                        questionScoreMap[q.Text] = sc;
                    }

                    scoreDto.MaxScore += maxScore;
                }

                scoreDto.SectionScores[sec.Category] = sectionScore;
                scoreDto.SectionQuestionScores[sec.Category] = questionScoreMap;
            }

            scoreDto.TotalScore = scoreDto.SectionScores.Sum(x => x.Value);
            a.ScoreJson = JsonSerializer.Serialize(scoreDto);

            // ---------------------------
            // Status update
            // ---------------------------
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

        // ---------------------------------------------------------
        // 4. VIEW ASSESSMENT (FULL DETAILS)
        // ---------------------------------------------------------
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

            var qjson = Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(qjson));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;

            return View("ViewAssessment", a);
        }

        // ---------------------------------------------------------
        // 5. REVIEW PAGE
        // ---------------------------------------------------------
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

            var qjson = Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(
                System.IO.File.ReadAllText(qjson));

            ViewBag.Sections = sections;
            ViewBag.Answers = answers;

            return View(a);
        }

        // ---------------------------------------------------------
        // 6. EXPORT FULL PDF
        // ---------------------------------------------------------
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

            var qf = Path.Combine(_env.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qf));

            var recFile = Path.Combine(_env.WebRootPath, "data", "recommendations.json");
            var recLib = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(System.IO.File.ReadAllText(recFile));

            // Charts
            string barChartRaw = Request.Form["barChartImage"];
            string doughnutChartRaw = Request.Form["doughnutChartImage"];

            byte[] barChart = string.IsNullOrWhiteSpace(barChartRaw) ? Array.Empty<byte>() :
                Convert.FromBase64String(barChartRaw);

            byte[] doughnutChart = string.IsNullOrWhiteSpace(doughnutChartRaw) ? Array.Empty<byte>() :
                Convert.FromBase64String(doughnutChartRaw);

            var pdf = new FullAssessmentPdfService()
                .Generate(a, score, sections, recLib, barChart, doughnutChart);

            return File(pdf, "application/pdf", $"Assessment_{a.Id}.pdf");
        }
    }
}




