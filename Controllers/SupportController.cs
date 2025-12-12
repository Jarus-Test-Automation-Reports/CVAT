using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
