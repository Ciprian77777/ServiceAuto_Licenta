using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;
namespace ServiceAutoLicenta.Controllers
{
    public class PieseController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;

        public PieseController(ServiceAutoLicentaContext context)
        {
            _context = context;
        }

        // GET: /Piese
        public async Task<IActionResult> Index(string? cautare, bool? stocScazut)
        {
            var piese = _context.Piese.AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                piese = piese.Where(p =>
                    p.Denumire.ToLower().Contains(cautare) ||
                    p.CodPiesa.ToLower().Contains(cautare) ||
                    (p.Producator != null && p.Producator.ToLower().Contains(cautare)));
            }

            if (stocScazut == true)
                piese = piese.Where(p => p.StocCurent <= p.StocMinim);

            ViewBag.Cautare = cautare;
            ViewBag.StocScazut = stocScazut;
            ViewBag.NrAlerte = await _context.Piese.CountAsync(p => p.StocCurent <= p.StocMinim);

            return View(await piese.OrderBy(p => p.Denumire).ToListAsync());
        }

        // GET: /Piese/Detalii/5
        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();

            var piesa = await _context.Piese
                .Include(p => p.LucrarePiese)
                    .ThenInclude(lp => lp.Lucrare)
                        .ThenInclude(l => l.Programare)
                            .ThenInclude(pr => pr.Masina)
                                .ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piesa == null) return NotFound();

            return View(piesa);
        }

        // GET: /Piese/Adauga
        public IActionResult Adauga()
        {
            return View();
        }

        // POST: /Piese/Adauga
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga([Bind("CodPiesa,Denumire,Producator,PretAchizitie,PretVanzare,StocCurent,StocMinim")] Piesa piesa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(piesa);
                await _context.SaveChangesAsync();
                TempData["Succes"] = $"Piesa '{piesa.Denumire}' a fost adăugată cu succes!";
                return RedirectToAction(nameof(Index));
            }
            return View(piesa);
        }

        // GET: /Piese/Editeaza/5
        public async Task<IActionResult> Editeaza(int? id)
        {
            if (id == null) return NotFound();

            var piesa = await _context.Piese.FindAsync(id);
            if (piesa == null) return NotFound();

            return View(piesa);
        }

        // POST: /Piese/Editeaza/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, [Bind("Id,CodPiesa,Denumire,Producator,PretAchizitie,PretVanzare,StocCurent,StocMinim")] Piesa piesa)
        {
            if (id != piesa.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(piesa);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = "Piesa a fost actualizată cu succes!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Piese.Any(p => p.Id == piesa.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(piesa);
        }

        // GET: /Piese/Sterge/5
        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();

            var piesa = await _context.Piese
                .Include(p => p.LucrarePiese)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piesa == null) return NotFound();

            return View(piesa);
        }

        // POST: /Piese/Sterge/5
        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var piesa = await _context.Piese
                .Include(p => p.LucrarePiese)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piesa == null) return NotFound();

            if (piesa.LucrarePiese.Any())
            {
                TempData["Eroare"] = "Piesa nu poate fi ștearsă deoarece a fost utilizată în lucrări!";
                return RedirectToAction(nameof(Index));
            }

            _context.Piese.Remove(piesa);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Piesa a fost ștearsă cu succes!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Piese/ActualizeazaStoc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizeazaStoc(int id, int cantitate)
        {
            var piesa = await _context.Piese.FindAsync(id);
            if (piesa == null) return NotFound();

            piesa.StocCurent += cantitate;
            if (piesa.StocCurent < 0) piesa.StocCurent = 0;

            await _context.SaveChangesAsync();
            TempData["Succes"] = $"Stocul a fost actualizat! Stoc curent: {piesa.StocCurent}";
            return RedirectToAction(nameof(Detalii), new { id });
        }
    }
}
