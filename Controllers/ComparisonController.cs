using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Data;
using CAT.AID.Web.Helpers;
using CAT.AID.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ComparisonReportVM = CAT.AID.Models.DTO.ComparisonReportVM;

namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class ComparisonController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;
       // private readonly PdfService _pdf;

        public ComparisonController(
            ApplicationDbContext db,
            IWebHostEnvironment environment
         //   PdfService pdf
        )
        {
            _db = db;
            _environment = environment;
            //_pdf = pdf;
        }

        public async Task<IActionResult> Index(int candidateId)
        {
            var comparisons = await _db.ComparisonMasters
                .Where(c => c.CandidateId == candidateId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.CandidateId = candidateId;
            return View(comparisons);
        }

        public async Task<IActionResult> StartComparison(int candidateId, List<int> assessmentIds)
        {
            if (assessmentIds.Count < 2)
                return BadRequest("Select at least two assessments");

            var master = new ComparisonMaster
            {
                CandidateId = candidateId,
                AssessmentIds = JsonSerializer.Serialize(assessmentIds),
                Status = "Draft",
                CreatedBy = Guid.Parse(User.FindFirst("sub").Value)
            };

            _db.ComparisonMasters.Add(master);
            await _db.SaveChangesAsync();
            return RedirectToAction("GenerateReport", new { id = master.Id });
        }

        public async Task<IActionResult> GenerateReport(int id)
        {
            var master = await _db.ComparisonMasters
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (master == null) return NotFound();

            var assessmentIds = JsonSerializer.Deserialize<List<int>>(master.AssessmentIds);
            var candidate = await _db.Candidates.FindAsync(master.CandidateId);

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            var assessments = await _db.Assessments
     .Where(a => assessmentIds.Contains(a.Id) &&
                (a.Status == AssessmentStatus.Approved ||
                 a.Status == AssessmentStatus.Submitted ||
                 a.Status == AssessmentStatus.InProgress))
     .OrderBy(a => a.CreatedAt)
     .ToListAsync();


            var rows = new List<ComparisonQuestionRowVM>();

            foreach (var sec in sections)
            {
                foreach (var q in sec.Questions)
                {
                    var row = new ComparisonQuestionRowVM
                    {
                        Domain = sec.Category,
                        QuestionText = q.Text
                    };

                    foreach (var a in assessments)
                    {
                        var answerDict = string.IsNullOrWhiteSpace(a.AssessmentResultJson)
                            ? new Dictionary<string, object>()
                            : JsonSerializer.Deserialize<Dictionary<string, object>>(a.AssessmentResultJson);

                        string scoreKey = $"SCORE_{q.Id}";
                        if (answerDict.ContainsKey(scoreKey))
                            row.Scores.Add(Convert.ToDouble(answerDict[scoreKey]));
                        else
                            row.Scores.Add(null);
                    }

                    if (row.Scores.Count >= 2)
                    {
                        var first = row.Scores.First();
                        var last = row.Scores.Last();
                        row.Difference = (first.HasValue && last.HasValue) ? (last - first) : null;
                    }

                    rows.Add(row);
                }
            }

            ViewBag.EvidenceFiles = await _db.ComparisonEvidences
                .Where(x => x.ComparisonId == master.Id)
                .ToListAsync();

            return View("CompareReport", new ComparisonReportVM
            {
                ComparisonId = master.Id,
                CandidateId = master.CandidateId,
                CandidateName = candidate.FullName,
                AssessmentIds = assessmentIds,
                Rows = rows,
                OverallComments = master.OverallComments,
                Status = master.Status,
                IsReviewMode = master.Status == "SentForReview"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Compare(int candidateId, List<int> ids)
        {
            if (candidateId == 0 || ids == null || ids.Count < 2)
            {
                TempData["msg"] = "⚠ Please select at least two completed assessments for comparison.";
                return RedirectToAction("MyTasks", "Assessments");
            }

            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId);
            if (candidate == null)
            {
                TempData["msg"] = "Candidate not found.";
                return RedirectToAction("MyTasks", "Assessments");
            }

            var assessments = await _db.Assessments
                .Where(a => ids.Contains(a.Id) && a.CandidateId == candidateId)
                .OrderBy(a => a.SubmittedAt)
                .ToListAsync();

            if (assessments.Count < 2)
            {
                TempData["msg"] = "⚠ No progress records found!";
                return RedirectToAction("MyTasks", "Assessments");
            }

            var qfile = Path.Combine(_environment.WebRootPath, "data", "assessment_questions.json");
            var sections = JsonSerializer.Deserialize<List<AssessmentSection>>(System.IO.File.ReadAllText(qfile));

            var rows = new List<ComparisonRowVM>();

            foreach (var sec in sections)
            {
                foreach (var q in sec.Questions)
                {
                    var row = new ComparisonRowVM
                    {
                        Domain = sec.Category,
                        Question = q.Text
                    };

                    foreach (var a in assessments)
                    {
                        if (!string.IsNullOrWhiteSpace(a.AssessmentResultJson))
                        {
                            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(a.AssessmentResultJson);
                            saved.TryGetValue($"SCORE_{q.Id}", out string score);
                            row.Scores.Add(score);
                        }
                    }

                    if (row.Scores.Count >= 2)
                    {
                        int first = int.Parse(row.Scores.First());
                        int last = int.Parse(row.Scores.Last());
                        row.Difference = (last - first).ToString();
                    }
                    else row.Difference = "-";

                    rows.Add(row);
                }
            }

            var vm = new ComparisonReportVM
            {
                CandidateId = candidateId,
                CandidateName = candidate.FullName,
                AssessmentIds = ids,
                Assessments = assessments
            };

            return View("CompareReport", vm);
        }

        [Authorize]
        public async Task<IActionResult> Dashboard(int candidateId)
        {
            var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.Id == candidateId);
            if (candidate == null) return NotFound();

            var comparisons = await _db.ComparisonMasters
                .Where(c => c.CandidateId == candidateId && c.Status == "Approved")
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.Candidate = candidate;

            if (!comparisons.Any())
                TempData["msg"] = "No approved comparison reports available for this candidate.";

            return View(comparisons);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDraft(ComparisonReportVM model)
        {
            var master = await _db.ComparisonMasters
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == model.ComparisonId);

            if (master == null) return NotFound();

            foreach (var row in model.Rows)
            {
                var detail = master.Details.FirstOrDefault(x => x.QuestionId == row.QuestionId)
                    ?? new ComparisonDetail { ComparisonId = master.Id, QuestionId = row.QuestionId };

                detail.Difference = (decimal?)row.Difference;
                detail.Notes = row.Notes;

                if (detail.Id == 0)
                    _db.ComparisonDetails.Add(detail);
            }

            master.OverallComments = model.OverallComments;
            master.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["msg"] = "Draft saved";
            return RedirectToAction("GenerateReport", new { id = master.Id });
        }

        [HttpPost]
        public async Task<IActionResult> SendForReview(int id)
        {
            var master = await _db.ComparisonMasters.FindAsync(id);
            if (master == null) return NotFound();

            master.Status = "SentForReview";
            master.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToAction("GenerateReport", new { id });
        }

        [Authorize(Roles = "Lead")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var master = await _db.ComparisonMasters.FindAsync(id);
            master.Status = "Approved";
            master.ReviewedBy = Guid.Parse(User.FindFirst("sub").Value);
            master.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction("GenerateReport", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var view = await GenerateReport(id) as ViewResult;
            if (view == null) return NotFound();

            string candidateName = view.Model is ComparisonReportVM vm
                ? vm.CandidateName.Replace(" ", "_")
                : "Report";

            var html = await this.RenderViewToStringAsync("CompareReport_Pdf", view.Model);

            // byte[] pdfBytes = _pdf.Generate(html);

            return File(
                // pdfBytes,
                "application/pdf",
                $"ComparisonReport_{candidateName}_{DateTime.Now:yyyyMMddHHmm}.pdf"
            );
        }

        [HttpPost]
        public async Task<IActionResult> UploadEvidence(int id, List<IFormFile> files)
        {
            foreach (var file in files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                _db.ComparisonEvidences.Add(new ComparisonEvidence
                {
                    ComparisonId = id,
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    FileData = ms.ToArray(),
                    UploadedBy = Guid.Parse(User.FindFirst("sub").Value),
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("GenerateReport", new { id });
        }

        public async Task<IActionResult> DownloadEvidence(int evidenceId)
        {
            var file = await _db.ComparisonEvidences.FindAsync(evidenceId);
            return File(file.FileData, file.FileType, file.FileName);
        }
    }
}


