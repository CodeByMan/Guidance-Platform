using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CareerGuidance.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CareerGuidance.Controllers
{
    public class ContactController : Controller
    {
        private readonly CareerDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ContactController(CareerDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Send(ContactMessage model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields correctly.";
                return RedirectToAction("Contact", "Home");
            }

            var identityUser = await _userManager.GetUserAsync(User);

            // Assuming your local Users table has a RoleId column
            var localUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (localUser == null || localUser.RoleId != 3) // 3 = User role
            {
                TempData["Error"] = "You are not authorized to send a message.";
                return RedirectToAction("Contact", "Home");
            }

            // Fill additional fields
            model.UserId = localUser.UserId.ToString();
            model.CreatedAt = DateTime.Now;

            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your message has been sent successfully!";
            return RedirectToAction("Contact", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewMessages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View(messages);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reply(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            return View(message);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reply(int id, string adminReply)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.AdminReply = adminReply;
            message.IsReplied = true;
            message.RepliedAt = DateTime.Now;

            _context.ContactMessages.Update(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reply sent successfully.";
            return RedirectToAction("ViewMessages");
        }

        [Authorize]
        public async Task<IActionResult> MyMessages()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            var localUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (localUser == null) return RedirectToAction("Index", "Home");

            var messages = await _context.ContactMessages
                .Where(m => m.UserId == localUser.UserId.ToString())
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(messages);
        }
    }
}
