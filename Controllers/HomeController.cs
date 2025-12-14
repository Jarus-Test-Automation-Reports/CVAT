using CAT.AID.Models;
using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CAT.AID.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        // Default route → redirect to assessment dashboard
        public IActionResult Index()
        {
            return RedirectToAction("MyTasks", "Assessments");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Temporary admin repair tool
        public async Task<IActionResult> FixAdmin()
        {
            var admin = await _userManager.FindByEmailAsync("admin@aid.com");
            if (admin == null)
                return Content("Admin user not found in AspNetUsers table.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
            var result = await _userManager.ResetPasswordAsync(admin, token, "Admin@123");

            if (result.Succeeded)
                return Content("✔ Admin password reset to Admin@123. You can now log in.");
            else
                return Content("❌ " + string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}
