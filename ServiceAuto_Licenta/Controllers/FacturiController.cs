using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    public class FacturiController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;

        public FacturiController(ServiceAutoLicentaContext context)
        {
            _context = context;
        }

        // GET: /Facturi
        public async Task<IActionResult> Index(string? cautare, string? status)
        {
            var facturi = _context.Facturi
                .Include(f => f.Programare)
                    .ThenInclude(p => p.Masina)
                        .ThenInclude(m => m.Client)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                facturi = facturi.Where(f =>
                    f.SerieNumar.ToLower().Contains(cautare) ||
                    f.Programare.Masina.Client.Nume.ToLower().Contains(cautare) ||
                    f.Programare.Masina.Client.Prenume.ToLower().Contains(cautare) ||
                    f.Programare.Masina.NrInmatriculare.ToLower().Contains(cautare));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPlata>(status, out var statusEnum))
                facturi = facturi.Where(f => f.StatusPlata == statusEnum);

            ViewBag.Cautare = cautare;
            ViewBag.Status = status;
            ViewBag.TotalIncasat = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita)
                .SumAsync(f => f.Total);

            return View(await facturi.OrderByDescending(f => f.DataEmitere).ToListAsync());
        }

        // GET: /Facturi/Detalii/5
        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare)
                    .ThenInclude(p => p.Masina)
                        .ThenInclude(m => m.Client)
                .Include(f => f.Programare)
                    .ThenInclude(p => p.Lucrari)
                        .ThenInclude(l => l.LucrarePiese)
                            .ThenInclude(lp => lp.Piesa)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        // GET: /Facturi/Genereaza?programareId=5
        public async Task<IActionResult> Genereaza(int? programareId)
        {
            if (programareId == null) return NotFound();

            var programare = await _context.Programari
                .Include(p => p.Masina)
                    .ThenInclude(m => m.Client)
                .Include(p => p.Lucrari)
                    .ThenInclude(l => l.LucrarePiese)
                        .ThenInclude(lp => lp.Piesa)
                .Include(p => p.Factura)
                .FirstOrDefaultAsync(p => p.Id == programareId);

            if (programare == null) return NotFound();

            if (programare.Factura != null)
            {
                TempData["Eroare"] = "Această programare are deja o factură generată!";
                return RedirectToAction(nameof(Detalii), new { id = programare.Factura.Id });
            }

            // Generează numărul facturii automat
            var ultimaFactura = await _context.Facturi
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            int numarNou = (ultimaFactura?.Id ?? 0) + 1;
            string serieNumar = $"SA-{DateTime.Now.Year}-{numarNou:D4}";

            var factura = new Factura
            {
                ProgramareId = programare.Id,
                SerieNumar = serieNumar,
                DataEmitere = DateTime.Today,
                DataScadenta = DateTime.Today.AddDays(30),
                StatusPlata = StatusPlata.Neplata,
                Subtotal = programare.TotalFaraTva,
                TvaValoare = programare.Tva,
                Total = programare.TotalCuTva
            };

            ViewBag.Programare = programare;
            return View(factura);
        }

        // POST: /Facturi/Genereaza
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Genereaza([Bind("ProgramareId,SerieNumar,DataEmitere,DataScadenta,StatusPlata,MetodaPlata,Subtotal,TvaValoare,Total")] Factura factura)
        {
            ModelState.Remove("Programare");

            if (ModelState.IsValid)
            {
                _context.Add(factura);
                await _context.SaveChangesAsync();
                TempData["Succes"] = $"Factura {factura.SerieNumar} a fost generată cu succes!";
                return RedirectToAction(nameof(Detalii), new { id = factura.Id });
            }

            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .FirstOrDefaultAsync(p => p.Id == factura.ProgramareId);

            ViewBag.Programare = programare;
            return View(factura);
        }

        // POST: /Facturi/ActualizeazaStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizeazaStatus(int id, StatusPlata statusPlata, MetodaPlata? metodaPlata)
        {
            var factura = await _context.Facturi.FindAsync(id);
            if (factura == null) return NotFound();

            factura.StatusPlata = statusPlata;
            factura.MetodaPlata = metodaPlata;
            await _context.SaveChangesAsync();

            TempData["Succes"] = "Statusul facturii a fost actualizat!";
            return RedirectToAction(nameof(Detalii), new { id });
        }

        // GET: /Facturi/Sterge/5
        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare)
                    .ThenInclude(p => p.Masina)
                        .ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        // POST: /Facturi/Sterge/5
        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var factura = await _context.Facturi.FindAsync(id);
            if (factura == null) return NotFound();

            if (factura.StatusPlata == StatusPlata.Platita)
            {
                TempData["Eroare"] = "Factura plătită nu poate fi ștearsă!";
                return RedirectToAction(nameof(Index));
            }

            _context.Facturi.Remove(factura);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Factura a fost ștearsă cu succes!";
            return RedirectToAction(nameof(Index));
        }

        
        // GET: /Facturi/PlataCard/5
        public async Task<IActionResult> PlataCard(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare)
                    .ThenInclude(p => p.Masina)
                        .ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            if (factura.StatusPlata == StatusPlata.Platita)
            {
                TempData["Eroare"] = "Factura este deja platita!";
                return RedirectToAction(nameof(Detalii), new { id });
            }

            return View(factura);
        }

        // POST: /Facturi/PlataCard/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlataCard(int id, string numarCard, string titular, string expirare, string cvv)
        {
            var factura = await _context.Facturi.FindAsync(id);
            if (factura == null) return NotFound();

            // Simulare procesare — generăm un ID de tranzacție
            await Task.Delay(1500); // simulăm timpul de procesare

            string idTranzactie = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            factura.StatusPlata = StatusPlata.Platita;
            factura.MetodaPlata = MetodaPlata.Card;

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Plata a fost procesata cu succes! ID tranzactie: {idTranzactie}";
            TempData["IdTranzactie"] = idTranzactie;
            return RedirectToAction(nameof(Detalii), new { id });
        }
    }
}