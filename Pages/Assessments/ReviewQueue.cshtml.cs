using CAT.AID.Models;
using CAT.AID.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CAT.AID.Web.Pages.Assessments
{
    public class ReviewQueueModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ReviewQueueModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Assessment> Assessments { get; set; } = new();

        public void OnGet()
        {
            Assessments = _db.Assessments
                .Include(a => a.Candidate)
                .OrderByDescending(a => a.Id)
                .ToList();
        }
    }
}
