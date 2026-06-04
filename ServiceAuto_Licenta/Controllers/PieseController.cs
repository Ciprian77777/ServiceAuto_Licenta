using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class PieseController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public PieseController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }

        public async Task<IActionResult> Index(string cautare, bool stocScazut)
        {
            string userId=userManager.GetUserId(User);

            var piese=db.Piese.Where(x=>x.UserId==userId);

            if(!string.IsNullOrEmpty(cautare))
            {
                cautare=cautare.ToLower();
                piese=piese.Where(x=>
                    x.Denumire.ToLower().Contains(cautare) ||
                    x.CodPiesa.ToLower().Contains(cautare) ||
                    x.Producator.ToLower().Contains(cautare));
            }

            if(stocScazut)
                piese=piese.Where(x=>x.StocCurent<=x.StocMinim);

            ViewBag.Cautare=cautare;
            ViewBag.StocScazut=stocScazut;
            ViewBag.NrAlerte=await db.Piese
                .CountAsync(x=>x.UserId==userId&&x.StocCurent<=x.StocMinim);

            return View(await piese.OrderBy(x=>x.Denumire).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int id)
        {
            string userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .Include(x=>x.LucrarePiese)
                    .ThenInclude(x=>x.Lucrare)
                        .ThenInclude(x=>x.Programare)
                            .ThenInclude(x=>x.Masina)
                                .ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);

            if(piesa==null) return NotFound();

            return View(piesa);
        }

        public IActionResult Adauga()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga(Piesa piesa)
        {
            string userId=userManager.GetUserId(User);
            ModelState.Remove("UserId");

            if(ModelState.IsValid)
            {
                try
                {
                    piesa.UserId=userId;
                    db.Piese.Add(piesa);
                    await db.SaveChangesAsync();
                    TempData["Succes"]="Piesa a fost adaugata cu succes!";
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    TempData["Eroare"]="A aparut o eroare. Verificati codul piesei.";
                }
            }

            return View(piesa);
        }

        public async Task<IActionResult> Editeaza(int id)
        {
            string userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);

            if(piesa==null) return NotFound();

            return View(piesa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, Piesa piesa)
        {
            string userId=userManager.GetUserId(User);
            ModelState.Remove("UserId");

            if(id!=piesa.Id) return NotFound();

            if(ModelState.IsValid)
            {
                try
                {
                    piesa.UserId=userId;
                    db.Piese.Update(piesa);
                    await db.SaveChangesAsync();
                    TempData["Succes"]="Piesa a fost actualizata!";
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    TempData["Eroare"]="A aparut o eroare la salvare.";
                }
            }

            return View(piesa);
        }

        public async Task<IActionResult> Sterge(int id)
        {
            string userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .Include(x=>x.LucrarePiese)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);

            if(piesa==null) return NotFound();

            return View(piesa);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            string userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .Include(x=>x.LucrarePiese)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);

            if(piesa==null) return NotFound();

            if(piesa.LucrarePiese.Any())
            {
                TempData["Eroare"]="Piesa nu poate fi stearsa deoarece a fost folosita in lucrari!";
                return RedirectToAction("Index");
            }

            db.Piese.Remove(piesa);
            await db.SaveChangesAsync();
            TempData["Succes"]="Piesa a fost stearsa!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizeazaStoc(int id, int cantitate)
        {
            string userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);

            if(piesa==null) return NotFound();

            piesa.StocCurent += cantitate;
            if(piesa.StocCurent < 0) piesa.StocCurent=0;

            await db.SaveChangesAsync();
            TempData["Succes"]=$"Stoc actualizat! Stoc curent: {piesa.StocCurent}";
            return RedirectToAction("Detalii", new { id });
        }
    }
}