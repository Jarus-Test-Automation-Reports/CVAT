using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Data;
using CAT.AID.Web.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CAT.AID.Web.Controllers
{
    [Authorize(Roles = "Lead, Admin, Assessor")]
    public class ProgressController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProgressController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 📌 View progress timeline
        public async Task<IActionResult> Progress(int id)
        {
            var assessments = await _db.Assessments
                .Where(a => a.CandidateId == id && a.Status == AssessmentStatus.Approved)
                .OrderBy(a => a.Id)
                .ToListAsync();

            if (!assessments.Any())
                return View("ProgressNoData");

            var progressList = new List<CandidateProgressDTO>();

            foreach (var a in assessments)
            {
                var score = string.IsNullOrEmpty(a.ScoreJson)
                    ? new AssessmentScoreDTO()
                    : JsonSerializer.Deserialize<AssessmentScoreDTO>(a.ScoreJson);

                progressList.Add(new CandidateProgressDTO
                {
                    AssessmentId = a.Id,
                    Date = a.ReviewedAt ?? a.SubmittedAt ?? a.CreatedAt,
                    Total = score.TotalScore,
                    Max = score.MaxScore,
                    Percentage = score.Percentage,
                    SectionScores = score.SectionScores
                });
            }

            ViewBag.Candidate = await _db.Candidates.FindAsync(id);
            return View(progressList);
        }

        // 📌 Progress Report PDF
        public async Task<IActionResult> ProgressReport(int id)
        {
            var candidate = await _db.Candidates.FindAsync(id);
            var history = await _db.Assessments
                .Where(a => a.CandidateId == id && a.Status == AssessmentStatus.Approved)
                .OrderBy(a => a.SubmittedAt)
                .ToListAsync();

            var pdf = ProgressReportGenerator.GeneratePdf(candidate, history);
            return File(pdf, "application/pdf",
                $"{candidate.FullName}_ProgressReport.pdf");
        }

        // 📌 Export detailed PDF
        [Authorize(Roles = "Lead, Admin")]
   public async Task<IActionResult> ExportProgress(int id)
   {
       var candidate = await _db.Candidates.FindAsync(id);
       var assessments = await _db.Assessments
           .Where(a => a.CandidateId == id && a.Status == AssessmentStatus.Approved)
           .OrderBy(a => a.Id)
           .ToListAsync();

       var pdf = ProgressReportGenerator.Build(candidate, assessments);
       return File(pdf, "application/pdf", $"Progress_{candidate.FullName}.pdf");
   }
    }
}


