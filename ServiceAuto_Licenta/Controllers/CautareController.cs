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
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CautareController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new SearchResult { Query = "" });

            var userId = GetUserId();
            var query = q.ToLower().Trim();

            var result = new SearchResult { Query = q };

            // Clienti
            result.Clienti = await _context.Clienti
                .Include(c => c.Masini)
                .Where(c => c.UserId == userId && (
                    c.Nume.ToLower().Contains(query) ||
                    c.Prenume.ToLower().Contains(query) ||
                    c.Telefon.Contains(query) ||
                    (c.Email != null && c.Email.ToLower().Contains(query)) ||
                    (c.Cnp != null && c.Cnp.Contains(query))))
                .Take(5)
                .ToListAsync();

            // Masini
            result.Masini = await _context.Masini
                .Include(m => m.Client)
                .Where(m => m.Client.UserId == userId && (
                    m.NrInmatriculare.ToLower().Contains(query) ||
                    m.Marca.ToLower().Contains(query) ||
                    m.ModelMasina.ToLower().Contains(query) ||
                    (m.Vin != null && m.Vin.ToLower().Contains(query))))
                .Take(5)
                .ToListAsync();

            // Programari
            result.Programari = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Where(p => p.Masina.Client.UserId == userId && (
                    p.Masina.NrInmatriculare.ToLower().Contains(query) ||
                    p.Masina.Client.Nume.ToLower().Contains(query) ||
                    p.Masina.Client.Prenume.ToLower().Contains(query) ||
                    (p.Observatii != null && p.Observatii.ToLower().Contains(query))))
                .OrderByDescending(p => p.DataIntrare)
                .Take(5)
                .ToListAsync();

            // Piese
            result.Piese = await _context.Piese
                .Where(p => p.UserId == userId && (
                    p.Denumire.ToLower().Contains(query) ||
                    p.CodPiesa.ToLower().Contains(query) ||
                    (p.Producator != null && p.Producator.ToLower().Contains(query))))
                .Take(5)
                .ToListAsync();

            // Facturi
            result.Facturi = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Where(f => f.Programare.Masina.Client.UserId == userId && (
                    f.SerieNumar.ToLower().Contains(query) ||
                    f.Programare.Masina.Client.Nume.ToLower().Contains(query) ||
                    f.Programare.Masina.Client.Prenume.ToLower().Contains(query) ||
                    f.Programare.Masina.NrInmatriculare.ToLower().Contains(query)))
                .OrderByDescending(f => f.DataEmitere)
                .Take(5)
                .ToListAsync();

            return View(result);
        }
    }

    public class SearchResult
    {
        public string Query { get; set; } = "";
        public List<Client> Clienti { get; set; } = new();
        public List<Masina> Masini { get; set; } = new();
        public List<Programare> Programari { get; set; } = new();
        public List<Piesa> Piese { get; set; } = new();
        public List<Factura> Facturi { get; set; } = new();

        public int TotalRezultate =>
            Clienti.Count + Masini.Count + Programari.Count + Piese.Count + Facturi.Count;
    }
}