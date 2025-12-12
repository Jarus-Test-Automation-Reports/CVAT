using CAT.AID.Models;
using CAT.AID.Web.Data;
using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CAT.AID.Web.Pages.Assessments
{
    public class ReviewAssessmentModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ReviewAssessmentModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Assessment Assessment { get; set; }

        public List<ApplicationUser> Assessors { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Assessment = _db.Assessments
                .Include(a => a.Candidate)
                .Include(a => a.Assessor)
                .FirstOrDefault(a => a.Id == id);

            if (Assessment == null) return NotFound();


            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

            Assessors = (await userManager.GetUsersInRoleAsync("Assessor"))
                .OrderBy(x => x.FullName)
                .ToList();


            return Page();
        }

        public IActionResult OnPost(string action, string? newAssessorId)
        {
            var data = _db.Assessments
                .Include(a => a.Candidate)
                .FirstOrDefault(a => a.Id == Assessment.Id);

            if (data == null) return NotFound();

            // Update lead comments always
            data.LeadComments = Assessment.LeadComments;
            if (!string.IsNullOrEmpty(newAssessorId))
            {
                data.AssessorId = newAssessorId;
                data.Status = AssessmentStatus.Assigned;
            }

            // Handle reassignment
            /*if (!string.IsNullOrEmpty(newAssessorId))
            {
                data.AssessorId = newAssessorId;
                data.Status = AssessmentStatus.Assigned;
            }*/

            switch (action)
            {
                case "approve":
                    data.Status = AssessmentStatus.Approved;
                    data.ReviewedAt = DateTime.UtcNow;
                    break;

                case "reject":
                    data.Status = AssessmentStatus.Rejected;
                    data.ReviewedAt = DateTime.UtcNow;
                    break;

                case "sendback":
                    data.Status = AssessmentStatus.SentBack;
                    break;

                case "lead-edit":
                    // allow lead to perform the assessment personally
                    data.Status = AssessmentStatus.InProgress;
                    break;
            }

            _db.SaveChanges();
            return RedirectToPage("/Assessments/ReviewQueue");
        }
    }
}
