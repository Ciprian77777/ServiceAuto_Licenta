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
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public ProgramariController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }

        public async Task<IActionResult> Index(string cautare, string status)
        {
            string? userId=userManager.GetUserId(User);

            var programari=db.Programari
                .Include(x=>x.Masina)
                .ThenInclude(x=>x.Client)
                .Where(x=>x.Masina.Client.UserId==userId);

            if(!string.IsNullOrEmpty(cautare))
            {
                cautare=cautare.ToLower();
                programari=programari.Where(x=>x.Masina.NrInmatriculare.ToLower().Contains(cautare)||x.Masina.Client.Nume.ToLower().Contains(cautare)||x.Masina.Client.Prenume.ToLower().Contains(cautare));
            }

            if(!string.IsNullOrEmpty(status)&&Enum.TryParse<StatusProgramare>(status, out var statusEnum))
                programari=programari.Where(x=>x.Status==statusEnum);

            ViewBag.Cautare=cautare;
            ViewBag.Status=status;

            return View(await programari.OrderByDescending(x=>x.DataIntrare).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int id)
        {
            string? userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese).ThenInclude(x=>x.Piesa)
                .Include(x=>x.Factura)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            ViewBag.Piese=await db.Piese
                .Where(x=>x.UserId==userId&&x.StocCurent > 0)
                .OrderBy(x=>x.Denumire)
                .ToListAsync();

            return View(programare);
        }

        public async Task<IActionResult> Adauga(int? masinaId)
        {
            string? userId=userManager.GetUserId(User);
            await PopulareDropdownMasini(userId, masinaId);

            var programare=new Programare {DataIntrare=DateTime.Today};
            if(masinaId.HasValue)
                programare.MasinaId=masinaId.Value;

            return View(programare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adauga(Programare programare)
        {
            string? userId=userManager.GetUserId(User);
            ModelState.Remove("Masina");
            ModelState.Remove("Lucrari");
            ModelState.Remove("Factura");

            if(ModelState.IsValid)
            {
                var masina=await db.Masini
                    .Include(x=>x.Client)
                    .FirstOrDefaultAsync(x=>x.Id==programare.MasinaId&&x.Client.UserId==userId);

                if(masina==null) return NotFound();

                db.Programari.Add(programare);
                await db.SaveChangesAsync();
                TempData["Succes"]="Programarea a fost adaugata!";
                return RedirectToAction("Detalii", new {id=programare.Id});
            }

            await PopulareDropdownMasini(userId, programare.MasinaId);
            return View(programare);
        }

        public async Task<IActionResult> Editeaza(int id)
        {
            string? userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            await PopulareDropdownMasini(userId, programare.MasinaId);
            return View(programare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editeaza(int id, Programare programare)
        {
            string? userId=userManager.GetUserId(User);
            ModelState.Remove("Masina");
            ModelState.Remove("Lucrari");
            ModelState.Remove("Factura");

            if(id!=programare.Id) return NotFound();

            if(ModelState.IsValid)
            {
                var lucrari=await db.Lucrari
                    .Include(x=>x.LucrarePiese)
                    .Where(x=>x.ProgramareId==id)
                    .ToListAsync();

                decimal totalFaraTva=0;
                foreach(var l in lucrari)
                {
                    totalFaraTva+=l.Manopera;
                    foreach(var lp in l.LucrarePiese)
                    totalFaraTva+=lp.Cantitate * lp.PretUnitar;
                }

                programare.TotalFaraTva=totalFaraTva;
                programare.Tva=Math.Round(totalFaraTva * 0.19m, 2);
                programare.TotalCuTva=totalFaraTva + programare.Tva;

                db.Programari.Update(programare);
                await db.SaveChangesAsync();
                TempData["Succes"]="Programarea a fost actualizata!";
                return RedirectToAction("Detalii", new {id=programare.Id});
            }

            await PopulareDropdownMasini(userId, programare.MasinaId);
            return View(programare);
        }

        public async Task<IActionResult> Sterge(int id)
        {
            string? userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Lucrari)
                .Include(x=>x.Factura)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            return View(programare);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            string? userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Include(x=>x.Lucrari)
                .Include(x=>x.Factura)
                .FirstOrDefaultAsync(x=>x.Id==id&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            if(programare.Factura!=null)
            {
                TempData["Eroare"]="Programarea nu poate fi stearsa deoarece are o factura emisa!";
                return RedirectToAction("Index");
            }

            db.Programari.Remove(programare);
            await db.SaveChangesAsync();
            TempData["Succes"]="Programarea a fost stearsa!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdaugaLucrare(int programareId, string denumire, string descriere, decimal manopera, decimal durataOre)
        {
            string? userId=userManager.GetUserId(User);

            var programare=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .FirstOrDefaultAsync(x=>x.Id==programareId&&x.Masina.Client.UserId==userId);

            if(programare==null) return NotFound();

            var lucrare=new Lucrare
            {
                ProgramareId=programareId,
                Denumire=denumire,
                Descriere=descriere,
                Manopera=manopera,
                DurataOre=durataOre
            };

            db.Lucrari.Add(lucrare);
            await db.SaveChangesAsync();
            await RecalculeazaTotal(programareId);

            TempData["Succes"]="Lucrarea a fost adaugata!";
            return RedirectToAction("Detalii", new {id=programareId});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StergeLucrare(int lucrareId, int programareId)
        {
            var lucrare=await db.Lucrari.FindAsync(lucrareId);
            if(lucrare!=null)
            {
                db.Lucrari.Remove(lucrare);
                await db.SaveChangesAsync();
                await RecalculeazaTotal(programareId);
                TempData["Succes"]="Lucrarea a fost stearsa!";
            }
            return RedirectToAction("Detalii", new {id=programareId});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdaugaPiesa(int lucrareId, int programareId, int piesaId, int cantitate)
        {
            string? userId=userManager.GetUserId(User);

            var piesa=await db.Piese
                .FirstOrDefaultAsync(x=>x.Id==piesaId&&x.UserId==userId);

            if(piesa==null)
            {
                TempData["Eroare"]="Piesa nu a fost gasita!";
                return RedirectToAction("Detalii", new {id=programareId});
            }

            if(piesa.StocCurent < cantitate)
            {
                TempData["Eroare"]=$"Stoc insuficient! Stoc disponibil: {piesa.StocCurent}";
                return RedirectToAction("Detalii", new {id=programareId});
            }

            var lucrarePiesa=new LucrarePiesa
            {
                LucrareId=lucrareId,
                PiesaId=piesaId,
                Cantitate=cantitate,
                PretUnitar=piesa.PretVanzare
            };

            piesa.StocCurent -= cantitate;
            db.LucrarePiese.Add(lucrarePiesa);
            await db.SaveChangesAsync();
            await RecalculeazaTotal(programareId);

            TempData["Succes"]="Piesa a fost adaugata!";
            return RedirectToAction("Detalii", new {id=programareId});
        }

        private async Task RecalculeazaTotal(int programareId)
        {
            var programare=await db.Programari
                .Include(x=>x.Lucrari).ThenInclude(x=>x.LucrarePiese)
                .FirstOrDefaultAsync(x=>x.Id==programareId);

            if(programare==null) return;

            decimal total=0;
            foreach(var l in programare.Lucrari)
            {
                total+=l.Manopera;
                foreach(var lp in l.LucrarePiese)
                total+=lp.Cantitate * lp.PretUnitar;
            }

            programare.TotalFaraTva=total;
            programare.Tva=Math.Round(total * 0.19m, 2);
            programare.TotalCuTva=total + programare.Tva;

            await db.SaveChangesAsync();
        }

        private async Task PopulareDropdownMasini(string? userId, int? selectedId=null)
        {
            var masini=await db.Masini
                .Include(x=>x.Client)
                .Where(x=>x.Client.UserId==userId)
                .OrderBy(x=>x.NrInmatriculare)
                .Select(x=>new{x.Id,Descriere=x.NrInmatriculare + " - " + x.Marca + " " + x.ModelMasina + " (" + x.Client.Nume + " " + x.Client.Prenume + ")"})
                .ToListAsync();

            ViewBag.MasinaId=new SelectList(masini, "Id", "Descriere", selectedId);
        }
    }
}