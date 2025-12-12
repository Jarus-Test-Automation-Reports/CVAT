using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CAT.AID.Web.Data
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Lead", "Assessor" };

            // Create roles if not exist
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            await CreateUser(userManager,
                email: "admin@cataid.com",
                password: "Test@123",
                role: "Admin",
                fullName: "System Administrator");

            await CreateUser(userManager,
                email: "lead@cataid.com",
                password: "Test@123",
                role: "Lead",
                fullName: "Lead Assessor");

            await CreateUser(userManager,
                email: "assessor@cataid.com",
                password: "Test@123",
                role: "Assessor",
                fullName: "Field Assessor");
        }

        private static async Task CreateUser(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role,
            string fullName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Location = "Default"  // avoid null DB crash
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception("User creation failed: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
