using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public HomeController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db = _db;
            userManager = _userManager;
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
        public async Task<IActionResult> Index()
        {
            string userId = userManager.GetUserId(User);

            ViewBag.TotalClienti = await db.Clienti
                .CountAsync(x => x.UserId == userId);
            ViewBag.TotalMasini = await db.Masini
                .CountAsync(x => x.Client.UserId == userId);
            ViewBag.TotalProgramari = await db.Programari
                .CountAsync(x => x.Masina.Client.UserId == userId);
            ViewBag.TotalPiese = await db.Piese
                .CountAsync(x => x.UserId == userId);
            ViewBag.FacturiNeplatite = await db.Facturi
                .CountAsync(x =>x.Programare.Masina.Client.UserId == userId && x.StatusPlata == StatusPlata.Neplata );
            ViewBag.PiesePutine = await db.Piese
                .CountAsync(x =>x.UserId == userId && x.StocCurent <= x.StocMinim);
            ViewBag.ProgramariLucru = await db.Programari
                .CountAsync(x =>x.Masina.Client.UserId == userId && x.Status == StatusProgramare.InLucru);

            ViewBag.Venit = await db.Facturi
                .Where(x => x.Programare.Masina.Client.UserId == userId && x.StatusPlata == StatusPlata.Platita )
                .SumAsync(x => (decimal?)x.Total) ?? 0;

            ViewBag.UltimeProgramari = await db.Programari
                .Include(x => x.Masina)
                .ThenInclude(x => x.Client)
                .Where(x => x.Masina.Client.UserId == userId)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimeFacturi = await db.Facturi
                .Include(x => x.Programare)
                .ThenInclude(x => x.Masina)
                .ThenInclude(x => x.Client)
                .Where(x => x.Programare.Masina.Client.UserId == userId)
                .Take(5)
                .ToListAsync();

            ViewBag.PieseStocMic = await db.Piese
                .Where(x => x.UserId == userId && x.StocCurent <= x.StocMinim )
                .Take(5)
                .ToListAsync();
            return View();
        }
    }
}