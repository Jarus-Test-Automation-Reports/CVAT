using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CAT.AID.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // --------------------------------------------------------
        // USERS LIST
        // --------------------------------------------------------
        public IActionResult Users()
        {
            return View(_userManager.Users.ToList());
        }

        // --------------------------------------------------------
        // CREATE USER
        // --------------------------------------------------------
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string password, string role)
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            if (!ModelState.IsValid)
                return View(model);

            model.UserName = model.Email; // Identity login consistency

            var result = await _userManager.CreateAsync(model, password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(model);
            }

            await _userManager.AddToRoleAsync(model, role);

            TempData["msg"] = "User created successfully!";
            return RedirectToAction(nameof(Users));
        }

        // --------------------------------------------------------
        // EDIT USER
        // --------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            ViewBag.Role = role;

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model, string role)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Location = model.Location;

            var update = await _userManager.UpdateAsync(user);

            if (!update.Succeeded)
            {
                foreach (var err in update.Errors)
                    ModelState.AddModelError("", err.Description);

                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            // Update role
            var existingRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, existingRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["msg"] = "User updated successfully!";
            return RedirectToAction(nameof(Users));
        }

        // --------------------------------------------------------
        // RESET PASSWORD
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            TempData["msg"] = result.Succeeded
                ? "Password reset successfully!"
                : string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Edit), new { id });
        }

        // --------------------------------------------------------
        // DELETE USER
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Prevent deleting main admins
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                TempData["msg"] = "Admin user cannot be deleted!";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.DeleteAsync(user);

            TempData["msg"] = "User deleted successfully!";
            return RedirectToAction(nameof(Users));
        }

        // --------------------------------------------------------
        // LOCK USER
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["msg"] = "Admins cannot be locked!";
                return RedirectToAction(nameof(Users));
            }

            user.LockoutEnd = DateTime.UtcNow.AddYears(50);
            await _userManager.UpdateAsync(user);

            TempData["msg"] = "User locked!";
            return RedirectToAction(nameof(Users));
        }

        // --------------------------------------------------------
        // UNLOCK USER
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            TempData["msg"] = "User unlocked!";
            return RedirectToAction(nameof(Users));
        }
    }
}
