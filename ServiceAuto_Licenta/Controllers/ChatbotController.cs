using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;
using System.Text;
using System.Text.Json;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ChatbotController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Json(new { success = false, message = "Mesajul nu poate fi gol." });

            var userId = GetUserId();
            var dataContext = await BuildDataContext(userId);
            var response = await AskOllama(request.Message, dataContext);

            return Json(new { success = true, message = response });
        }

        private async Task<string> BuildDataContext(string userId)
        {
            var sb = new StringBuilder();

            // Clienti
            var clienti = await _context.Clienti
                .Include(c => c.Masini)
                    .ThenInclude(m => m.Programari)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            sb.AppendLine("=== CLIENTI ===");
            foreach (var c in clienti)
                sb.AppendLine($"- {c.NumeComplet} | Tel: {c.Telefon} | Email: {c.Email ?? "N/A"} | Masini: {c.Masini.Count} | Inregistrat: {c.CreatedAt:dd.MM.yyyy}");

            // Masini
            var masini = await _context.Masini
                .Include(m => m.Client)
                .Include(m => m.Programari)
                .Where(m => m.Client.UserId == userId)
                .ToListAsync();

            sb.AppendLine("\n=== MASINI ===");
            foreach (var m in masini)
                sb.AppendLine($"- {m.Marca} {m.ModelMasina} ({m.AnFabricatie}) | Nr: {m.NrInmatriculare} | Proprietar: {m.Client.NumeComplet} | Km: {m.KmActuali?.ToString("N0") ?? "N/A"} | Programari: {m.Programari.Count}");

            // Programari recente
            var programari = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari)
                .Where(p => p.Masina.Client.UserId == userId)
                .OrderByDescending(p => p.DataIntrare)
                .Take(20)
                .ToListAsync();

            sb.AppendLine("\n=== PROGRAMARI RECENTE ===");
            foreach (var p in programari)
                sb.AppendLine($"- #{p.Id} | {p.Masina.NrInmatriculare} ({p.Masina.Client.NumeComplet}) | Data: {p.DataIntrare:dd.MM.yyyy} | Status: {p.Status} | Total: {p.TotalCuTva:N2} lei | Lucrari: {string.Join(", ", p.Lucrari.Select(l => l.Denumire))}");

            // Facturi
            var facturi = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Where(f => f.Programare.Masina.Client.UserId == userId)
                .OrderByDescending(f => f.DataEmitere)
                .Take(20)
                .ToListAsync();

            sb.AppendLine("\n=== FACTURI ===");
            foreach (var f in facturi)
                sb.AppendLine($"- {f.SerieNumar} | Client: {f.Programare.Masina.Client.NumeComplet} | Total: {f.Total:N2} lei | Status: {f.StatusPlata} | Data: {f.DataEmitere:dd.MM.yyyy}");

            // Piese
            var piese = await _context.Piese
                .Where(p => p.UserId == userId)
                .ToListAsync();

            sb.AppendLine("\n=== PIESE INVENTAR ===");
            foreach (var p in piese)
            {
                var stocStatus = p.StocCurent == 0 ? "EPUIZAT" : p.StocScazut ? "STOC SCAZUT" : "OK";
                sb.AppendLine($"- {p.Denumire} | Cod: {p.CodPiesa} | Stoc: {p.StocCurent}/{p.StocMinim} ({stocStatus}) | Pret: {p.PretVanzare:N2} lei");
            }

            // Statistici
            var totalVenituri = facturi.Where(f => f.StatusPlata == StatusPlata.Platita).Sum(f => f.Total);
            var facturiNeincasate = facturi.Count(f => f.StatusPlata == StatusPlata.Neplata);
            var programariInLucru = programari.Count(p => p.Status == StatusProgramare.InLucru);
            var pieseStocScazut = piese.Count(p => p.StocScazut);

            sb.AppendLine($"\n=== STATISTICI ===");
            sb.AppendLine($"- Total clienti: {clienti.Count}");
            sb.AppendLine($"- Total masini: {masini.Count}");
            sb.AppendLine($"- Programari in lucru: {programariInLucru}");
            sb.AppendLine($"- Venituri incasate: {totalVenituri:N2} lei");
            sb.AppendLine($"- Facturi neincasate: {facturiNeincasate}");
            sb.AppendLine($"- Piese cu stoc scazut: {pieseStocScazut}");

            return sb.ToString();
        }

        private async Task<string> AskOllama(string userMessage, string dataContext)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            var prompt = $@"Esti un asistent virtual pentru un service auto. Ai acces la urmatoarele date din sistem:
 
{dataContext}
 
Raspunde la intrebarile utilizatorului bazandu-te EXCLUSIV pe datele de mai sus.
Raspunde in romana, concis si clar.
Daca informatia nu exista in date, spune ca nu ai gasit-o.
Nu inventa date.
Foloseste un ton profesional dar prietenos.
Cand listezi mai multe elemente, foloseste liste cu liniuta (-).
Nu mentiona ca ai un context sau date furnizate.
 
Intrebarea utilizatorului: {userMessage}
 
Raspuns:";

            var requestBody = new
            {
                model = "llama3.2",
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    num_predict = 500
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:11434/api/generate", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "Ne pare rau, a aparut o eroare. Verifica ca Ollama ruleaza pe calculatorul tau.";

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("response")
                    .GetString();

                return text?.Trim() ?? "Nu am putut genera un raspuns.";
            }
            catch (HttpRequestException)
            {
                return "Nu pot conecta la Ollama. Asigura-te ca Ollama ruleaza — deschide Command Prompt si scrie: ollama serve";
            }
            catch (TaskCanceledException)
            {
                return "Raspunsul a durat prea mult. Incearca o intrebare mai simpla.";
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
