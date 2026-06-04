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
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public ChatbotController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if(string.IsNullOrEmpty(request.Message))
                return Json(new { success=false });
            string userId=userManager.GetUserId(User);
            string data=await BuildData(userId);
            string raspuns=await AskOllama(request.Message, data);
            return Json(new
            {
                success=true,
                message=raspuns
            });
        }

        private async Task<string> BuildData(string userId)
        {
            StringBuilder sb=new StringBuilder();
            var clienti=await db.Clienti
                .Include(x=>x.Masini)
                .Where(x=>x.UserId==userId)
                .ToListAsync();

            sb.AppendLine("CLIENTI");
            foreach(var c in clienti)
            {
                sb.AppendLine( c.NumeComplet + " | " + c.Telefon);
            }

            var masini=await db.Masini
                .Include(x=>x.Client)
                .Where(x=>x.Client.UserId==userId)
                .ToListAsync();
            sb.AppendLine("MASINI");

            foreach(var m in masini)
            {
                sb.AppendLine( m.Marca + " " +m.ModelMasina + " | " + m.NrInmatriculare );
            }
            var programari=await db.Programari
                .Include(x=>x.Masina)
                .ThenInclude(x=>x.Client)
                .Where(x=>x.Masina.Client.UserId==userId)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("PROGRAMARI");

            foreach(var p in programari)
            {
                sb.AppendLine( p.Masina.NrInmatriculare + " | " + p.DataIntrare.ToString("dd.MM.yyyy") );
            }

            var facturi=await db.Facturi
                .Include(x=>x.Programare)
                .ThenInclude(x=>x.Masina)
                .ThenInclude(x=>x.Client)
                .Where(x=>x.Programare.Masina.Client.UserId==userId)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("FACTURI");

            foreach(var f in facturi)
            {
                sb.AppendLine( f.SerieNumar +" | " + f.Total +" lei" );
            }

            var piese=await db.Piese
                .Where(x=>x.UserId==userId)
                .ToListAsync();
            sb.AppendLine("PIESE");

            foreach(var p in piese)
            {
                sb.AppendLine(p.Denumire +" | Stoc: " +p.StocCurent);
            }

            sb.AppendLine("STATISTICI");
            sb.AppendLine("Clienti: " + clienti.Count);
            sb.AppendLine("Masini: " + masini.Count);
            sb.AppendLine("Programari: " + programari.Count);
            return sb.ToString();
        }

        private async Task<string> AskOllama(string mesaj, string data)
        {
            HttpClient client=new HttpClient();
            var body=new
            {
                model="llama3.2",
                prompt="Raspunde in romana folosind datele:\n\n" + data +  "\n\nIntrebare: " + mesaj,
                stream=false
            };

            var json=JsonSerializer.Serialize(body);
            var content=new StringContent(json,Encoding.UTF8, "application/json" );

            try
            {
                var response=await client.PostAsync("http://localhost:11434/api/generate",content);
                var text=await response.Content.ReadAsStringAsync();
                using JsonDocument doc=JsonDocument.Parse(text);
                return doc.RootElement
                    .GetProperty("response")
                    .GetString();
            }
            catch
            {
                return "Eroare la conectare.";
            }
        }
    }
    public class ChatRequest
    {
        public string Message { get; set; }
    }
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}