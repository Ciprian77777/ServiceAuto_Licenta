using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class ClientiController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public ClientiController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db = _db;
            userManager = _userManager;
        }

        public async Task<IActionResult> Index(string cautare)
        {
            string userId = userManager.GetUserId(User);

            var clienti = db.Clienti
                .Include(x => x.Masini)
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                clienti = clienti.Where(x =>
                    x.Nume.ToLower().Contains(cautare) ||
                    x.Prenume.ToLower().Contains(cautare) ||
                    x.Telefon.Contains(cautare) ||
                    x.Email.ToLower().Contains(cautare));
            }

            ViewBag.Cautare = cautare;
            return View(await clienti.OrderBy(x => x.Nume).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int id)
        {
            string userId = userManager.GetUserId(User);

            var client = await db.Clienti
                .Include(x => x.Masini)
                    .ThenInclude(x => x.Programari)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (client == null) return NotFound();

            return View(client);
        }

        public IActionResult Adauga()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga(Client client)
        {
            string userId = userManager.GetUserId(User);
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    client.UserId = userId;
                    db.Clienti.Add(client);
                    await db.SaveChangesAsync();
                    TempData["Succes"] = "Clientul a fost adaugat cu succes!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["Eroare"] = "A aparut o eroare la salvare. Verificati datele introduse.";
                }
            }

            return View(client);
        }

        public async Task<IActionResult> Editeaza(int id)
        {
            string userId = userManager.GetUserId(User);

            var client = await db.Clienti
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (client == null) return NotFound();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, Client client)
        {
            string userId = userManager.GetUserId(User);
            ModelState.Remove("UserId");

            if (id != client.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    client.UserId = userId;
                    db.Clienti.Update(client);
                    await db.SaveChangesAsync();
                    TempData["Succes"] = "Clientul a fost actualizat!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["Eroare"] = "A aparut o eroare la salvare.";
                }
            }

            return View(client);
        }

        public async Task<IActionResult> Sterge(int id)
        {
            string userId = userManager.GetUserId(User);

            var client = await db.Clienti
                .Include(x => x.Masini)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (client == null) return NotFound();

            return View(client);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            string userId = userManager.GetUserId(User);

            var client = await db.Clienti
                .Include(x => x.Masini)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (client == null) return NotFound();

            if (client.Masini.Any())
            {
                TempData["Eroare"] = "Clientul nu poate fi sters deoarece are masini inregistrate!";
                return RedirectToAction("Index");
            }

            db.Clienti.Remove(client);
            await db.SaveChangesAsync();
            TempData["Succes"] = "Clientul a fost sters!";
            return RedirectToAction("Index");
        }
    }
}