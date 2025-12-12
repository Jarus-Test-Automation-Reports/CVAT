using Microsoft.AspNetCore.Identity;

namespace CAT.AID.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Location { get; set; }
    }
}
