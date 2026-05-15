using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ServiceAutoLicenta.Controllers
{
    public class LandingController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public LandingController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Landing
        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Landing/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["LoginError"] = "Introduceti email-ul si parola.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["LoginError"] = "Email sau parola incorecta.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            TempData["LoginError"] = "Email sau parola incorecta.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Landing/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nume, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["RegisterError"] = "Parolele nu coincid.";
                TempData["ShowRegister"] = true;
                return RedirectToAction(nameof(Index));
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["RegisterError"] = "Exista deja un cont cu acest email.";
                TempData["ShowRegister"] = true;
                return RedirectToAction(nameof(Index));
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Succes"] = $"Bun venit, {nume}! Contul tau a fost creat.";
                return RedirectToAction("Index", "Home");
            }

            TempData["RegisterError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            TempData["ShowRegister"] = true;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Landing/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}