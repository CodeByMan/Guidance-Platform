using CareerGuidance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerExplorer.Controllers
{
    public class CareerController : Controller
    {
        private readonly CareerDbContext _context;

        public CareerController(CareerDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CareerMap()
        {
            var paths = await _context.CareerPaths.ToListAsync();
            return View(paths);
        }


        public async Task<IActionResult> JobRoles()
        {
            var roles = await _context.JobRoles.ToListAsync();
            return View(roles);
        }
    }
}
