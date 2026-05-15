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
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientiController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        // Detalii client
        public async Task<IActionResult> Index(string? cautare)
        {
            var userId = GetUserId();
            var clienti = _context.Clienti
                .Where(c => c.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                clienti = clienti.Where(c =>
                    c.Nume.ToLower().Contains(cautare) ||
                    c.Prenume.ToLower().Contains(cautare) ||
                    c.Telefon.Contains(cautare) ||
                    (c.Email != null && c.Email.ToLower().Contains(cautare)));
            }

            ViewBag.Cautare = cautare;
            return View(await clienti.Include(c => c.Masini).OrderBy(c => c.Nume).ToListAsync());
        }

        // Detalii client service
        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clienti
                .Include(c => c.Masini)
                    .ThenInclude(m => m.Programari)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetUserId());

            if (client == null) return NotFound();

            return View(client);
        }

        // Adauga
        public IActionResult Adauga() => View();

        // Adauga client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga([Bind("Nume,Prenume,Telefon,Email,Adresa,Cnp")] Client client)
        {
            ModelState.Remove("UserId");
            if (ModelState.IsValid)
            {
                try
                {
                    client.UserId = GetUserId();
                    _context.Add(client);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = $"Clientul {client.NumeComplet} a fost adăugat!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    TempData["Eroare"] = "Eroare: CNP-ul introdus există deja în sistem!";
                    return View(client);
                }
            }
            return View(client);
        }

        // Edit
        public async Task<IActionResult> Editeaza(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clienti
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetUserId());
            if (client == null) return NotFound();
            return View(client);
        }

        // Edit client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, [Bind("Id,Nume,Prenume,Telefon,Email,Adresa,Cnp")] Client client)
        {
            if (id != client.Id) return NotFound();

            ModelState.Remove("UserId");
            if (ModelState.IsValid)
            {
                try
                {
                    client.UserId = GetUserId();
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = $"Clientul {client.NumeComplet} a fost actualizat!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clienti.Any(c => c.Id == client.Id && c.UserId == GetUserId()))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Sterge
        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clienti
                .Include(c => c.Masini)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetUserId());
            if (client == null) return NotFound();
            return View(client);
        }


        // Sterge client
        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var client = await _context.Clienti
                .Include(c => c.Masini)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetUserId());

            if (client == null) return NotFound();

            if (client.Masini.Any())
            {
                TempData["Eroare"] = "Clientul nu poate fi șters deoarece are mașini înregistrate!";
                return RedirectToAction(nameof(Index));
            }

            _context.Clienti.Remove(client);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Clientul a fost șters!";
            return RedirectToAction(nameof(Index));
        }


    }
}