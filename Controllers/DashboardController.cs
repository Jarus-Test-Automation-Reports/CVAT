using CAT.AID.Models;
using CAT.AID.Web.Data;
using CAT.AID.Web.Helpers;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
  //  private readonly PdfService _pdf;

    public DashboardController(ApplicationDbContext db)//, PdfService pdf)
    {
        _db = db;
       // _pdf = pdf;
    }

    public async Task<IActionResult> Index()
    {
        var dto = new DashboardDTO
        {
            TotalAssessments = await _db.Assessments.CountAsync(),
            SubmittedCount = await _db.Assessments.CountAsync(a => a.Status == AssessmentStatus.Submitted),
            PendingCount = await _db.Assessments.CountAsync(a => a.Status == AssessmentStatus.Assigned || a.Status == AssessmentStatus.InProgress),
            ApprovedCount = await _db.Assessments.CountAsync(a => a.Status == AssessmentStatus.Approved)
        };

        // MONTHLY TREND
        var trend = await _db.Assessments
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .Select(g => new { Label = $"{g.Key.Month}-{g.Key.Year}", Count = g.Count() })
            .OrderBy(x => x.Label).Take(6).ToListAsync();

        dto.MonthLabels = trend.Select(x => x.Label).ToList();
        dto.MonthCounts = trend.Select(x => x.Count).ToList();

        // ASSESSOR PERFORMANCE
        var assessors = await _db.Assessments
            .Include(a => a.Assessor)
            .Where(a => a.Assessor != null)
            .GroupBy(a => a.Assessor.FullName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        dto.AssessorNames = assessors.Select(a => a.Name).ToList();
        dto.AssessorCounts = assessors.Select(a => a.Count).ToList();

        // LOW PERFORMING DOMAINS
        var approvedScores = await _db.Assessments
            .Where(a => a.ScoreJson != null)
            .Select(a => a.ScoreJson)
            .ToListAsync();

        var domainResults = new Dictionary<string, List<double>>();

        foreach (var json in approvedScores)
        {
            var score = JsonSerializer.Deserialize<AssessmentScoreDTO>(json);
            foreach (var sec in score.SectionScores)
            {
                if (!domainResults.ContainsKey(sec.Key))
                    domainResults[sec.Key] = new List<double>();

                domainResults[sec.Key].Add(sec.Value);
            }
        }

        dto.LowDomains = domainResults
            .ToDictionary(
                x => x.Key,
                x => x.Value.Count > 0 ? x.Value.Average() : 0
            )
            .Where(x => x.Value < 60)                 // BELOW THRESHOLD
            .OrderBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Value);

        // ACTIVITY TIMELINE
        var recent = await _db.Assessments
            .Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new { Date = g.Key.ToString("dd MMM"), Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        dto.RecentDates = recent.Select(r => r.Date).ToList();
        dto.RecentCounts = recent.Select(r => r.Count).ToList();

        return View(dto);
    }

    // Export Dashboard PDF
  
}


