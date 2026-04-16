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
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MasiniController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index(string? cautare)
        {
            var userId = GetUserId();
            var masini = _context.Masini
                .Include(m => m.Client)
                .Where(m => m.Client.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                masini = masini.Where(m =>
                    m.NrInmatriculare.ToLower().Contains(cautare) ||
                    m.Marca.ToLower().Contains(cautare) ||
                    m.ModelMasina.ToLower().Contains(cautare) ||
                    m.Client.Nume.ToLower().Contains(cautare) ||
                    m.Client.Prenume.ToLower().Contains(cautare));
            }

            ViewBag.Cautare = cautare;
            return View(await masini.Include(m => m.Programari).OrderBy(m => m.NrInmatriculare).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();
            var masina = await _context.Masini
                .Include(m => m.Client)
                .Include(m => m.Programari)
                .FirstOrDefaultAsync(m => m.Id == id && m.Client.UserId == GetUserId());
            if (masina == null) return NotFound();
            return View(masina);
        }

        public async Task<IActionResult> Adauga(int? clientId)
        {
            await PopulateClientiDropdown(clientId);
            var masina = new Masina();
            if (clientId.HasValue) masina.ClientId = clientId.Value;
            return View(masina);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga([Bind("ClientId,Marca,ModelMasina,AnFabricatie,NrInmatriculare,Vin,KmActuali")] Masina masina)
        {
            ModelState.Remove("Client");
            if (ModelState.IsValid)
            {
                // verificam ca clientul apartine userului
                var client = await _context.Clienti
                    .FirstOrDefaultAsync(c => c.Id == masina.ClientId && c.UserId == GetUserId());
                if (client == null) return NotFound();

                _context.Add(masina);
                await _context.SaveChangesAsync();
                TempData["Succes"] = $"Masina {masina.Marca} {masina.ModelMasina} a fost adaugata!";
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientiDropdown(masina.ClientId);
            return View(masina);
        }

        public async Task<IActionResult> Editeaza(int? id)
        {
            if (id == null) return NotFound();
            var masina = await _context.Masini
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.Id == id && m.Client.UserId == GetUserId());
            if (masina == null) return NotFound();
            await PopulateClientiDropdown(masina.ClientId);
            return View(masina);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, [Bind("Id,ClientId,Marca,ModelMasina,AnFabricatie,NrInmatriculare,Vin,KmActuali")] Masina masina)
        {
            if (id != masina.Id) return NotFound();
            ModelState.Remove("Client");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(masina);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = "Masina a fost actualizata!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Masini.Any(m => m.Id == masina.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientiDropdown(masina.ClientId);
            return View(masina);
        }

        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();
            var masina = await _context.Masini
                .Include(m => m.Client)
                .Include(m => m.Programari)
                .FirstOrDefaultAsync(m => m.Id == id && m.Client.UserId == GetUserId());
            if (masina == null) return NotFound();
            return View(masina);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var masina = await _context.Masini
                .Include(m => m.Client)
                .Include(m => m.Programari)
                .FirstOrDefaultAsync(m => m.Id == id && m.Client.UserId == GetUserId());
            if (masina == null) return NotFound();
            if (masina.Programari.Any())
            {
                TempData["Eroare"] = "Masina nu poate fi stearsa deoarece are programari!";
                return RedirectToAction(nameof(Index));
            }
            _context.Masini.Remove(masina);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Masina a fost stearsa!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClientiDropdown(int? selectedClientId = null)
        {
            var userId = GetUserId();
            var clienti = await _context.Clienti
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Nume)
                .Select(c => new { c.Id, NumeComplet = c.Nume + " " + c.Prenume })
                .ToListAsync();
            ViewBag.ClientId = new SelectList(clienti, "Id", "NumeComplet", selectedClientId);
        }
    }
}