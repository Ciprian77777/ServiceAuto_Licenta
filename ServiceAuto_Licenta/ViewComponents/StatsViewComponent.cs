using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.ViewComponents
{
    public class StatsModel
    {
        public int TotalClienti{get;set;}
        public int TotalMasini{get;set;}
        public int Programate{get;set;}
        public int InLucru{get;set;}
        public int Finalizate{get;set;}
        public int FacturiNeplatite{get;set;}
        public int AlerteStoc{get;set;}
        public decimal VenituriLuna{get;set;}
    }

    [ViewComponent(Name = "Stats")]
    public class StatsViewComponent : ViewComponent
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public StatsViewComponent(ServiceAutoLicentaContext db, UserManager<IdentityUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = userManager.GetUserId(HttpContext.User);
            if (string.IsNullOrEmpty(userId))
                return View(new StatsModel());

            var primaZiLuna = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var model = new StatsModel
            {
                TotalClienti= await db.Clienti.CountAsync(x => x.UserId == userId),
                TotalMasini = await db.Masini.CountAsync(x => x.Client.UserId == userId),
                Programate= await db.Programari.CountAsync(x => x.Masina.Client.UserId == userId && x.Status == StatusProgramare.Programata),
                InLucru=await db.Programari.CountAsync(x => x.Masina.Client.UserId == userId && x.Status == StatusProgramare.InLucru),
                Finalizate = await db.Programari.CountAsync(x => x.Masina.Client.UserId == userId && x.Status == StatusProgramare.Finalizata),
                FacturiNeplatite = await db.Facturi.CountAsync(x => x.Programare.Masina.Client.UserId == userId && x.StatusPlata == StatusPlata.Neplata),
                AlerteStoc = await db.Piese.CountAsync(x => x.UserId == userId && x.StocCurent <= x.StocMinim),
                VenituriLuna = await db.Facturi
                    .Where(x => x.Programare.Masina.Client.UserId == userId && x.StatusPlata == StatusPlata.Platita && x.DataEmitere >= primaZiLuna)
                    .SumAsync(x => (decimal?)x.Total) ?? 0
            };

            HttpContext.Items["AlerteGlobale"] = model.AlerteStoc;

            return View(model);
        }
    }
}
