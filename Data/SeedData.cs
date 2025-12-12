using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CAT.AID.Web.Data
{
    public static class SeedData
    {
        private static readonly string[] Roles = new[] { "Admin", "LeadAssessor", "Assessor" };

        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // ----- 1️⃣ CREATE ROLES -----
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ----- 2️⃣ CREATE DEFAULT USERS -----
            await CreateUser(userManager, "admin@cataid.com", "Admin", "System Administrator", "Test@123");
            await CreateUser(userManager, "lead@cataid.com", "LeadAssessor", "Lead Assessor", "Test@123");
            await CreateUser(userManager, "assessor@cataid.com", "Assessor", "Assessment Officer", "Test@123");
        }


        private static async Task CreateUser(
            UserManager<ApplicationUser> userManager,
            string email,
            string role,
            string fullName,
            string password)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    FullName = fullName,
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    Location = "Default"
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        $"❌ Failed to create seed user '{email}': " +
                        $"{string.Join("; ", result.Errors.Select(e => e.Description))}"
                    );
                }
            }

            // ----- 3️⃣ ASSIGN ROLE IF NOT ASSIGNED -----
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
