using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class FacturiController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public FacturiController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }

        public async Task<IActionResult> Index(string cautare, string status)
        {
            string userId=userManager.GetUserId(User);

            var facturi=db.Facturi
                .Include(x=>x.Programare)
                    .ThenInclude(x=>x.Masina)
                        .ThenInclude(x=>x.Client)
                .Where(x=>x.Programare.Masina.Client.UserId==userId);

            if(!string.IsNullOrEmpty(cautare))
            {
                cautare=cautare.ToLower();
                facturi=facturi.Where(x=>
                    x.SerieNumar.ToLower().Contains(cautare) ||
                    x.Programare.Masina.Client.Nume.ToLower().Contains(cautare) ||
                    x.Programare.Masina.Client.Prenume.ToLower().Contains(cautare) ||
                    x.Programare.Masina.NrInmatriculare.ToLower().Contains(cautare));
            }

            if(!string.IsNullOrEmpty(status)&&Enum.TryParse<StatusPlata>(status, out var statusEnum))
                facturi=facturi.Where(x=>x.StatusPlata==statusEnum);

            ViewBag.Cautare=cautare;
            ViewBag.Status=status;
            ViewBag.TotalIncasat=await db.Facturi
                .Where(x=>x.Programare.Masina.Client.UserId==userId&&x.StatusPlata==StatusPlata.Platita)
                .SumAsync(x=>(decimal?)x.Total)??0;

            return View(await facturi.OrderByDescending(x=>x.DataEmitere).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int id)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare)
                    .ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Programare)
                    .ThenInclude(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese).ThenInclude(x=>x.Piesa)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            return View(factura);
        }

        public async Task<IActionResult> Genereaza(int programareId)
        {
            string userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese).ThenInclude(x=>x.Piesa)
                .Include(x=>x.Factura)
                .FirstOrDefaultAsync(x=>x.Id==programareId&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            if(programare.Factura!=null)
            {
                TempData["Eroare"]="Aceasta programare are deja o factura!";
                return RedirectToAction("Detalii", new { id=programare.Factura.Id });
            }

            var ultimaFactura=await db.Facturi
                .Where(x=>x.Programare.Masina.Client.UserId==userId)
                .OrderByDescending(x=>x.Id)
                .FirstOrDefaultAsync();

            int numar=(ultimaFactura?.Id??0) + 1;

            var factura=new Factura
            {
                ProgramareId=programare.Id,
                SerieNumar=$"SA-{DateTime.Now.Year}-{numar:D4}",
                DataEmitere=DateTime.Today,
                DataScadenta=DateTime.Today.AddDays(30),
                StatusPlata=StatusPlata.Neplata,
                Subtotal=programare.TotalFaraTva,
                TvaValoare=programare.Tva,
                Total=programare.TotalCuTva
            };

            ViewBag.Programare=programare;
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Genereaza(Factura factura)
        {
            ModelState.Remove("Programare");

            if(ModelState.IsValid)
            {
                db.Facturi.Add(factura);
                await db.SaveChangesAsync();
                TempData["Succes"]=$"Factura {factura.SerieNumar} a fost generata!";
                return RedirectToAction("Detalii", new { id=factura.Id });
            }

            string userId=userManager.GetUserId(User);
            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese).ThenInclude(x=>x.Piesa)
                .FirstOrDefaultAsync(x=>x.Id==factura.ProgramareId);

            ViewBag.Programare=programare;
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizeazaStatus(int id, StatusPlata statusPlata, MetodaPlata? metodaPlata)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            factura.StatusPlata=statusPlata;
            factura.MetodaPlata=metodaPlata;
            await db.SaveChangesAsync();

            TempData["Succes"]="Statusul facturii a fost actualizat!";
            return RedirectToAction("Detalii", new { id });
        }

        public async Task<IActionResult> PlataCard(int id)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            if(factura.StatusPlata==StatusPlata.Platita)
            {
                TempData["Eroare"]="Factura este deja platita!";
                return RedirectToAction("Detalii", new { id });
            }

            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlataCard(int id, string numarCard, string titular, string expirare, string cvv)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            await Task.Delay(1500);

            string idTranzactie=$"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            factura.StatusPlata=StatusPlata.Platita;
            factura.MetodaPlata=MetodaPlata.Card;
            await db.SaveChangesAsync();

            TempData["Succes"]=$"Plata procesata cu succes! ID tranzactie: {idTranzactie}";
            return RedirectToAction("Detalii", new { id });
        }

        public async Task<IActionResult> Print(int id)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Programare).ThenInclude(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese).ThenInclude(x=>x.Piesa)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            return View(factura);
        }

        public async Task<IActionResult> Sterge(int id)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            return View(factura);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            string userId=userManager.GetUserId(User);

            var factura=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Programare.Masina.Client.UserId==userId);

            if(factura==null) return NotFound();

            if(factura.StatusPlata==StatusPlata.Platita)
            {
                TempData["Eroare"]="Factura platita nu poate fi stearsa!";
                return RedirectToAction("Index");
            }

            db.Facturi.Remove(factura);
            await db.SaveChangesAsync();
            TempData["Succes"]="Factura a fost stearsa!";
            return RedirectToAction("Index");
        }
    }
}