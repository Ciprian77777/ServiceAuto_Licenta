using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class CautareController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public CautareController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }
        public async Task<IActionResult> Index(string q)
        {
            SearchResult r=new SearchResult();
            if(string.IsNullOrEmpty(q))
                return View(r);

            string? userId=userManager.GetUserId(User);
            q=q.ToLower();
            r.Query=q;
            r.Clienti=await db.Clienti
                .Include(x=>x.Masini)
                .Where(x=>x.UserId==userId &&(x.Nume.ToLower().Contains(q)||x.Prenume.ToLower().Contains(q)||x.Telefon.Contains(q)))
                .Take(5)
                .ToListAsync();

            r.Masini=await db.Masini
                .Include(x=>x.Client)
                .Where(x=>x.Client.UserId==userId &&(x.Marca.ToLower().Contains(q)||x.ModelMasina.ToLower().Contains(q) ||x.NrInmatriculare.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            r.Programari=await db.Programari
                .Include(x=>x.Masina)
                .ThenInclude(x=>x.Client)
                .Where(x=>x.Masina.Client.UserId==userId&&(x.Masina.NrInmatriculare.ToLower().Contains(q)|| x.Masina.Client.Nume.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            r.Piese=await db.Piese
                .Where(x=>x.UserId==userId&&(x.Denumire.ToLower().Contains(q)|| x.CodPiesa.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            r.Facturi=await db.Facturi
                .Include(x=>x.Programare)
                .ThenInclude(x=>x.Masina)
                .ThenInclude(x=>x.Client)
                .Where(x=>x.Programare.Masina.Client.UserId==userId &&( x.SerieNumar.ToLower().Contains(q) ||x.Programare.Masina.NrInmatriculare.ToLower().Contains(q) ))
                .Take(5)
                .ToListAsync();
            return View(r);
        }
    }

    public class SearchResult
    {
        public string? Query {get;set;}
        public List<Client> Clienti {get;set;}=new List<Client>();
        public List<Masina> Masini {get;set;}=new List<Masina>();
        public List<Programare> Programari {get;set;}=new List<Programare>();
        public List<Piesa> Piese {get;set;}=new List<Piesa>();
        public List<Factura> Facturi {get;set;}=new List<Factura>();
        public int TotalRezultate=>Clienti.Count + Masini.Count + Programari.Count + Piese.Count + Facturi.Count;
    }
}