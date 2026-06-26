using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ServiceAutoLicenta.Controllers
{
    public class LandingController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IEmailSender emailSender;

        public LandingController(
            UserManager<IdentityUser> _userManager,
            SignInManager<IdentityUser> _signInManager,
            IEmailSender _emailSender)
        {
            userManager=_userManager;
            signInManager=_signInManager;
            emailSender=_emailSender;
        }

        public IActionResult Index()
        {
            if(signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if(string.IsNullOrEmpty(email)||string.IsNullOrEmpty(password))
            {
                TempData["LoginError"]="Completati toate campurile.";
                return RedirectToAction("Index");
            }
            var user=await userManager.FindByEmailAsync(email);
            if(user==null)
            {
                TempData["LoginError"]="Email sau parola incorecte.";
                return RedirectToAction("Index");
            }
            if(!await userManager.IsEmailConfirmedAsync(user))
            {
                TempData["LoginError"]="Contul nu a fost confirmat. Verificati emailul.";
                return RedirectToAction("Index");
            }
            var result=await signInManager.PasswordSignInAsync(user,password,false,false);
            if(result.Succeeded)
                return RedirectToAction("Index", "Home");
            TempData["LoginError"]="Email sau parola incorecte.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string nume, string email, string password, string confirmPassword)
        {
            if(password!=confirmPassword)
            {
                TempData["RegisterError"]="Parolele nu coincid.";
                return RedirectToAction("Index");
            }
            var userExist=await userManager.FindByEmailAsync(email);
            if(userExist!=null)
            {
                TempData["RegisterError"]="Exista deja un cont cu acest email.";
                return RedirectToAction("Index");
            }
            var user=new IdentityUser { UserName=email, Email=email };
            var result=await userManager.CreateAsync(user, password);
            if(result.Succeeded)
            {
                var token=await userManager.GenerateEmailConfirmationTokenAsync(user);
                var link=Url.Action("ConfirmEmail", "Landing", new { userId=user.Id, token }, Request.Scheme);
                await emailSender.SendEmailAsync(email, "Confirmare cont GarageCare",
                    $"<p>Salut {nume},</p>" +
                    $"<p>Apasa butonul de mai jos pentru a-ti confirma contul.</p>" +
                    $"<a href='{link}' style='background:#c1272d;color:white;padding:10px 24px;text-decoration:none;font-weight:bold;'>Confirma contul</a>" +
                    $"<p style='margin-top:16px;color:#888;font-size:12px;'>Daca nu ai creat acest cont, ignora acest email.</p>");
                return View("VerificaEmail");
            }
            TempData["RegisterError"]="A aparut o eroare la creare cont.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            if(string.IsNullOrEmpty(userId)||string.IsNullOrEmpty(token))
                return RedirectToAction("Index");
            var user=await userManager.FindByIdAsync(userId);
            if(user==null)
                return RedirectToAction("Index");
            var result=await userManager.ConfirmEmailAsync(user, token);
            ViewBag.Succes=result.Succeeded;
            return View("ConfirmareEmail");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }
    }
}
