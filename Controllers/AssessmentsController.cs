using CAT.AID.Models;
using CAT.AID.Models.DTO;
using CAT.AID.Web.Data;
using CAT.AID.Web.Models;
using CAT.AID.Web.Models.DTO;
using CAT.AID.Web.Services;
using CAT.AID.Web.Services.Pdf;
using CAT.AID.Web.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CAT.AID.Web.Controllers
{
    [Authorize]
    public class AssessmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IWebHostEnvironment _environment;

        public AssessmentsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> user,
            IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _environment = env;
        }
