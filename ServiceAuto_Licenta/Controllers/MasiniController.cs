using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class MasiniController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public MasiniController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }

        public async Task<IActionResult> Index(string cautare)
        {
            string userId=userManager.GetUserId(User);

            var masini=db.Masini
                .Include(x=>x.Client)
                .Include(x=>x.Programari)
                .Where(x=>x.Client.UserId==userId);

            if(!string.IsNullOrEmpty(cautare))
            {
                cautare=cautare.ToLower();
                masini=masini.Where(x=>
                    x.NrInmatriculare.ToLower().Contains(cautare) ||
                    x.Marca.ToLower().Contains(cautare) ||
                    x.ModelMasina.ToLower().Contains(cautare) ||
                    x.Client.Nume.ToLower().Contains(cautare) ||
                    x.Client.Prenume.ToLower().Contains(cautare));
            }

            ViewBag.Cautare=cautare;
            return View(await masini.OrderBy(x=>x.NrInmatriculare).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int id)
        {
            string userId=userManager.GetUserId(User);

            var masina=await db.Masini
                .Include(x=>x.Client)
                .Include(x=>x.Programari)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Client.UserId==userId);

            if(masina==null) return NotFound();

            return View(masina);
        }

        public async Task<IActionResult> Adauga(int? clientId)
        {
            string userId=userManager.GetUserId(User);
            await PopulareDropdownClienti(userId, clientId);

            var masina=new Masina();
            if(clientId.HasValue)
                masina.ClientId=clientId.Value;

            return View(masina);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga(Masina masina)
        {
            string userId=userManager.GetUserId(User);
            ModelState.Remove("Client");

            if(ModelState.IsValid)
            {
                try
                {
                    var client=await db.Clienti
                        .FirstOrDefaultAsync(x=>x.Id==masina.ClientId&&x.UserId==userId);

                    if(client==null) return NotFound();

                    db.Masini.Add(masina);
                    await db.SaveChangesAsync();
                    TempData["Succes"]="Masina a fost adaugata cu succes!";
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    TempData["Eroare"]="A aparut o eroare. Verificati datele introduse.";
                }
            }

            await PopulareDropdownClienti(userId, masina.ClientId);
            return View(masina);
        }

        public async Task<IActionResult> Editeaza(int id)
        {
            string userId=userManager.GetUserId(User);

            var masina=await db.Masini
                .Include(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Client.UserId==userId);

            if(masina==null) return NotFound();

            await PopulareDropdownClienti(userId, masina.ClientId);
            return View(masina);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, Masina masina)
        {
            string userId=userManager.GetUserId(User);
            ModelState.Remove("Client");

            if(id!=masina.Id) return NotFound();

            if(ModelState.IsValid)
            {
                try
                {
                    db.Masini.Update(masina);
                    await db.SaveChangesAsync();
                    TempData["Succes"]="Masina a fost actualizata!";
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    TempData["Eroare"]="A aparut o eroare la salvare.";
                }
            }

            await PopulareDropdownClienti(userId, masina.ClientId);
            return View(masina);
        }

        public async Task<IActionResult> Sterge(int id)
        {
            string userId=userManager.GetUserId(User);

            var masina=await db.Masini
                .Include(x=>x.Client)
                .Include(x=>x.Programari)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Client.UserId==userId);

            if(masina==null) return NotFound();

            return View(masina);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            string userId=userManager.GetUserId(User);

            var masina=await db.Masini
                .Include(x=>x.Client)
                .Include(x=>x.Programari)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Client.UserId==userId);

            if(masina==null) return NotFound();

            if(masina.Programari.Any())
            {
                TempData["Eroare"]="Masina nu poate fi stearsa deoarece are programari!";
                return RedirectToAction("Index");
            }

            db.Masini.Remove(masina);
            await db.SaveChangesAsync();
            TempData["Succes"]="Masina a fost stearsa!";
            return RedirectToAction("Index");
        }

        private async Task PopulareDropdownClienti(string userId, int? selectedId=null)
        {
            var clienti=await db.Clienti
                .Where(x=>x.UserId==userId)
                .OrderBy(x=>x.Nume)
                .Select(x=>new { x.Id, Nume=x.Nume + " " + x.Prenume })
                .ToListAsync();

            ViewBag.ClientId=new SelectList(clienti, "Id", "Nume", selectedId);
        }
    }
}