using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ServiceAutoLicenta.Controllers
{
    public class LandingController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;

        public LandingController(
            UserManager<IdentityUser> _userManager,
            SignInManager<IdentityUser> _signInManager)
        {
            userManager = _userManager;
            signInManager = _signInManager;
        }

        public IActionResult Index()
        {
            if (signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return RedirectToAction("Index");
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction("Index");
            var result = await signInManager.PasswordSignInAsync(
                user,
                password,
                false,
                false
            );

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string nume,
            string email,
            string password,
            string confirmPassword)
        {
            if (password != confirmPassword)
                return RedirectToAction("Index");
            var userExist = await userManager.FindByEmailAsync(email);

            if (userExist != null)
                return RedirectToAction("Index");

            IdentityUser user = new IdentityUser();

            user.UserName = email;
            user.Email = email;

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }
    }
}