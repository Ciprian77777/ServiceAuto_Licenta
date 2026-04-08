using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceAutoLicenta.Models.Entities
{
    public class Client
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        [Display(Name = "Nume")]
        public string Nume { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Prenume")]
        public string Prenume { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Telefon")]
        public string Telefon { get; set; } = string.Empty;

        [MaxLength(150), EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [MaxLength(150)]
        [Display(Name = "Adresa")]
        public string? Adresa { get; set; }

        [MaxLength(13)]
        [Display(Name = "CNP")]
        public string? Cnp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Masina> Masini { get; set; } = new List<Masina>();
        [NotMapped]
        public string NumeComplet => $"{Nume} {Prenume}";
    }
}
