using CAT.AID.Models;
using Microsoft.AspNetCore.Identity;

namespace CAT.AID.Web.Models.ViewModels
{
    public class AssignViewModel
    {
        public List<Assessment> Assessments { get; set; } = new();
        public List<ApplicationUser> Assessors { get; set; } = new();
    }
}
