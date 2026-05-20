using CareerGuidance.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CareerGuidance.Controllers
{
    public class AuthController : Controller
    {
        private readonly CareerDbContext db;

        public AuthController(CareerDbContext _db)
        {
            db = _db;
        }

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                TempData["ErrorMessage"] = "Please fill all required fields.";
                return RedirectToAction("Signup");
            }

            // Get confirm password from form
            string confirmPassword = Request.Form["ConfirmPassword"];

            if (user.Password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match!";
                return RedirectToAction("Signup");
            }

            var checkUser = await db.Users.FirstOrDefaultAsync(a => a.Email == user.Email);
            if (checkUser != null)
            {
                TempData["ErrorMessage"] = "Email already taken. Try another email.";
                return RedirectToAction("Signup");
            }

            // Hash the password
            var hasher = new PasswordHasher<string>();
            string hashPassword = hasher.HashPassword(user.Email, user.Password);
            user.Password = hashPassword;

            // Add user
            db.Users.Add(user);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Signup successful! Please login.";
            return View();
        }



        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                TempData["ErrorMessage"] = "Invalid Credentials";
                return RedirectToAction("Login");
            }

            var checkUser = await db.Users.FirstOrDefaultAsync(a => a.Email == user.Email);
            if (checkUser != null)
            {
                var hasher = new PasswordHasher<string>();
                var verifyPass = hasher.VerifyHashedPassword(user.Email, checkUser.Password, user.Password);

                if (verifyPass == PasswordVerificationResult.Success)
                {
                    // Set session
                    HttpContext.Session.SetInt32("UserID", checkUser.UserId);
                    HttpContext.Session.SetString("UserEmail", checkUser.Email);
                    HttpContext.Session.SetString("UserRole", GetRoleName(checkUser.RoleId));

                    // Sign in with Claims
                    var identity = new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.Name, checkUser.Username),
                new Claim(ClaimTypes.Role, GetRoleName(checkUser.RoleId)),
                new Claim(ClaimTypes.NameIdentifier, checkUser.UserId.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme);

                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Redirect to dashboard based on RoleId
                    switch (checkUser.RoleId)
                    {
                        case 1:
                            return RedirectToAction("Index", "Admin");
                        case 2:
                            return RedirectToAction("Index", "Mentor");
                        case 3:
                            return RedirectToAction("Index", "Home");
                        default:
                            // Unknown role, redirect to login
                            TempData["ErrorMessage"] = "Invalid Role";
                            return RedirectToAction("Login");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid Credentials";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid Credentials";
                return RedirectToAction("Login");
            }
        }



        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                1 => "Admin",
                2 => "Mentor",
                3 => "User",
                _ => "Unknown"
            };
        }
    }
}
