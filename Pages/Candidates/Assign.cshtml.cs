using CAT.AID.Models;
using CAT.AID.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CAT.AID.Web.Pages.Candidates
{
    public class AssignModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public AssignModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Assessment> Assessments { get; set; } = new();

        public void OnGet()
        {
            Assessments = _db.Assessments
                .Include(a => a.Candidate)
                .OrderBy(a => a.Status)
                .ToList();
        }
    }
}
