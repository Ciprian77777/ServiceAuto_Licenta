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
    public class ProgramariController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProgramariController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index(string? cautare, string? status)
        {
            var userId = GetUserId();
            var programari = _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Where(p => p.Masina.Client.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                programari = programari.Where(p =>
                    p.Masina.NrInmatriculare.ToLower().Contains(cautare) ||
                    p.Masina.Client.Nume.ToLower().Contains(cautare) ||
                    p.Masina.Client.Prenume.ToLower().Contains(cautare));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusProgramare>(status, out var statusEnum))
                programari = programari.Where(p => p.Status == statusEnum);

            ViewBag.Cautare = cautare;
            ViewBag.Status = status;

            return View(await programari.OrderByDescending(p => p.DataIntrare).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();

            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .Include(p => p.Factura)
                .FirstOrDefaultAsync(p => p.Id == id && p.Masina.Client.UserId == GetUserId());

            if (programare == null) return NotFound();

            ViewBag.Piese = await _context.Piese
                .Where(p => p.UserId == GetUserId() && p.StocCurent > 0)
                .OrderBy(p => p.Denumire)
                .ToListAsync();

            return View(programare);
        }

        public async Task<IActionResult> Adauga(int? masinaId)
        {
            await PopulateMasiniDropdown(masinaId);
            var programare = new Programare { DataIntrare = DateTime.Today };
            if (masinaId.HasValue) programare.MasinaId = masinaId.Value;
            return View(programare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga([Bind("MasinaId,DataIntrare,DataIesire,Status,Observatii")] Programare programare)
        {
            ModelState.Remove("Masina");
            ModelState.Remove("Lucrari");
            ModelState.Remove("Factura");

            if (ModelState.IsValid)
            {
                // verificam ca masina apartine userului
                var masina = await _context.Masini
                    .Include(m => m.Client)
                    .FirstOrDefaultAsync(m => m.Id == programare.MasinaId && m.Client.UserId == GetUserId());
                if (masina == null) return NotFound();

                _context.Add(programare);
                await _context.SaveChangesAsync();
                TempData["Succes"] = "Programarea a fost adaugata!";
                return RedirectToAction(nameof(Detalii), new { id = programare.Id });
            }
            await PopulateMasiniDropdown(programare.MasinaId);
            return View(programare);
        }

        public async Task<IActionResult> Editeaza(int? id)
        {
            if (id == null) return NotFound();
            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(p => p.Id == id && p.Masina.Client.UserId == GetUserId());
            if (programare == null) return NotFound();
            await PopulateMasiniDropdown(programare.MasinaId);
            return View(programare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, [Bind("Id,MasinaId,DataIntrare,DataIesire,Status,Observatii")] Programare programare)
        {
            if (id != programare.Id) return NotFound();
            ModelState.Remove("Masina");
            ModelState.Remove("Lucrari");
            ModelState.Remove("Factura");

            if (ModelState.IsValid)
            {
                try
                {
                    var lucrari = await _context.Lucrari
                        .Include(l => l.LucrarePiese)
                        .Where(l => l.ProgramareId == id)
                        .ToListAsync();

                    decimal totalFaraTva = lucrari.Sum(l =>
                        l.Manopera + l.LucrarePiese.Sum(lp => lp.Cantitate * lp.PretUnitar));

                    programare.TotalFaraTva = totalFaraTva;
                    programare.Tva = Math.Round(totalFaraTva * 0.19m, 2);
                    programare.TotalCuTva = totalFaraTva + programare.Tva;

                    _context.Update(programare);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = "Programarea a fost actualizata!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Programari.Any(p => p.Id == programare.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Detalii), new { id = programare.Id });
            }
            await PopulateMasiniDropdown(programare.MasinaId);
            return View(programare);
        }

        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();
            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari)
                .Include(p => p.Factura)
                .FirstOrDefaultAsync(p => p.Id == id && p.Masina.Client.UserId == GetUserId());
            if (programare == null) return NotFound();
            return View(programare);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari)
                .Include(p => p.Factura)
                .FirstOrDefaultAsync(p => p.Id == id && p.Masina.Client.UserId == GetUserId());

            if (programare == null) return NotFound();

            if (programare.Factura != null)
            {
                TempData["Eroare"] = "Programarea nu poate fi stearsa deoarece are o factura emisa!";
                return RedirectToAction(nameof(Index));
            }

            _context.Programari.Remove(programare);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Programarea a fost stearsa!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdaugaLucrare(int programareId, string denumire, string? descriere, decimal manopera, decimal durataOre)
        {
            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(p => p.Id == programareId && p.Masina.Client.UserId == GetUserId());
            if (programare == null) return NotFound();

            var lucrare = new Lucrare
            {
                ProgramareId = programareId,
                Denumire = denumire,
                Descriere = descriere,
                Manopera = manopera,
                DurataOre = durataOre
            };

            _context.Lucrari.Add(lucrare);
            await _context.SaveChangesAsync();
            await RecalculeazaTotal(programareId);

            TempData["Succes"] = "Lucrarea a fost adaugata!";
            return RedirectToAction(nameof(Detalii), new { id = programareId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StergeLucrare(int lucrareId, int programareId)
        {
            var lucrare = await _context.Lucrari.FindAsync(lucrareId);
            if (lucrare != null)
            {
                _context.Lucrari.Remove(lucrare);
                await _context.SaveChangesAsync();
                await RecalculeazaTotal(programareId);
                TempData["Succes"] = "Lucrarea a fost stearsa!";
            }
            return RedirectToAction(nameof(Detalii), new { id = programareId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdaugaPiesa(int lucrareId, int programareId, int piesaId, int cantitate)
        {
            var piesa = await _context.Piese
                .FirstOrDefaultAsync(p => p.Id == piesaId && p.UserId == GetUserId());

            if (piesa == null)
            {
                TempData["Eroare"] = "Piesa nu a fost gasita!";
                return RedirectToAction(nameof(Detalii), new { id = programareId });
            }

            if (piesa.StocCurent < cantitate)
            {
                TempData["Eroare"] = $"Stoc insuficient! Stoc disponibil: {piesa.StocCurent}";
                return RedirectToAction(nameof(Detalii), new { id = programareId });
            }

            var lucrarePiesa = new LucrarePiesa
            {
                LucrareId = lucrareId,
                PiesaId = piesaId,
                Cantitate = cantitate,
                PretUnitar = piesa.PretVanzare
            };

            piesa.StocCurent -= cantitate;
            _context.LucrarePiese.Add(lucrarePiesa);
            await _context.SaveChangesAsync();
            await RecalculeazaTotal(programareId);

            TempData["Succes"] = "Piesa a fost adaugata!";
            return RedirectToAction(nameof(Detalii), new { id = programareId });
        }

        private async Task RecalculeazaTotal(int programareId)
        {
            var programare = await _context.Programari
                .Include(p => p.Lucrari).ThenInclude(l => l.LucrarePiese)
                .FirstOrDefaultAsync(p => p.Id == programareId);

            if (programare == null) return;

            decimal totalFaraTva = programare.Lucrari.Sum(l =>
                l.Manopera + l.LucrarePiese.Sum(lp => lp.Cantitate * lp.PretUnitar));

            programare.TotalFaraTva = totalFaraTva;
            programare.Tva = Math.Round(totalFaraTva * 0.19m, 2);
            programare.TotalCuTva = totalFaraTva + programare.Tva;

            await _context.SaveChangesAsync();
        }

        private async Task PopulateMasiniDropdown(int? selectedMasinaId = null)
        {
            var userId = GetUserId();
            var masini = await _context.Masini
                .Include(m => m.Client)
                .Where(m => m.Client.UserId == userId)
                .OrderBy(m => m.NrInmatriculare)
                .Select(m => new
                {
                    m.Id,
                    Descriere = m.NrInmatriculare + " - " + m.Marca + " " + m.ModelMasina + " (" + m.Client.Nume + " " + m.Client.Prenume + ")"
                })
                .ToListAsync();

            ViewBag.MasinaId = new SelectList(masini, "Id", "Descriere", selectedMasinaId);
        }
    }
}