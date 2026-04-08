using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;
namespace ServiceAutoLicenta.Controllers
{
        public class ClientiController : Controller
        {
            private readonly ServiceAutoLicentaContext _context;

            public ClientiController(ServiceAutoLicentaContext context)
            {
                _context = context;
            }

            public async Task<IActionResult> Index(string? cautare)
            {
                var clienti = _context.Clienti.AsQueryable();

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
                return View(await clienti.OrderBy(c => c.Nume).ToListAsync());
            }

            public async Task<IActionResult> Detalii(int? id)
            {
                if (id == null) return NotFound();

                var client = await _context.Clienti
                    .Include(c => c.Masini)
                        .ThenInclude(m => m.Programari)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return NotFound();

                return View(client);
            }

            public IActionResult Adauga()
            {
                return View();
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Adauga([Bind("Nume,Prenume,Telefon,Email,Adresa,Cnp")] Client client)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(client);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = $"Clientul {client.NumeComplet} a fost adaugat cu succes!";
                    return RedirectToAction(nameof(Index));
                }
                return View(client);
            }

            public async Task<IActionResult> Editeaza(int? id)
            {
                if (id == null) return NotFound();

                var client = await _context.Clienti.FindAsync(id);
                if (client == null) return NotFound();

                return View(client);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Editeaza(int id, [Bind("Id,Nume,Prenume,Telefon,Email,Adresa,Cnp")] Client client)
            {
                if (id != client.Id) return NotFound();

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(client);
                        await _context.SaveChangesAsync();
                        TempData["Succes"] = $"Clientul {client.NumeComplet} a fost actualizat!";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!_context.Clienti.Any(c => c.Id == client.Id))
                            return NotFound();
                        throw;
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(client);
            }

            // GET: /Clienti/Sterge/5
            public async Task<IActionResult> Sterge(int? id)
            {
                if (id == null) return NotFound();

                var client = await _context.Clienti
                    .Include(c => c.Masini)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return NotFound();

                return View(client);
            }

            // POST: /Clienti/Sterge/5
            [HttpPost, ActionName("Sterge")]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ConfirmaStergere(int id)
            {
                var client = await _context.Clienti
                    .Include(c => c.Masini)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return NotFound();

                if (client.Masini.Any())
                {
                    TempData["Eroare"] = "Clientul nu poate fi sters deoarece are masini inregistrate!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Clienti.Remove(client);
                await _context.SaveChangesAsync();
                TempData["Succes"] = "Clientul a fost sters cu succes!";
                return RedirectToAction(nameof(Index));
            }
        }
    
}
