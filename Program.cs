using CAT.AID.Web.Data;
using CAT.AID.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// DATABASE CONFIG
// -------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// -------------------------------------------------------
// IDENTITY CONFIG
// -------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

// -------------------------------------------------------
// MVC + RAZOR
// -------------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// -------------------------------------------------------
// AUTO MIGRATIONS + SEEDING (REQUIRED FOR RENDER)
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // 🔥 Apply migrations automatically on startup
        db.Database.Migrate();

        // 🔥 Seed initial roles/users
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedData.InitializeAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Migration/Seeding failed: " + ex.Message);
        Console.WriteLine(ex.StackTrace);
        throw;
    }
}

// -------------------------------------------------------
// MIDDLEWARE PIPELINE
// -------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// -------------------------------------------------------
// DEFAULT ROUTE
// -------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
