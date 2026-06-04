using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceAutoLicenta.Models.Entities
{
    public class Lucrare
    {
        public int Id { get; set; }

        [Required]
        public int ProgramareId { get; set; }

        [Required, MaxLength(200)]
        [Display(Name="Denumire")]
        public string Denumire { get; set; }=string.Empty;

        [Display(Name="Descriere")]
        public string? Descriere { get; set; }

        [Required, Column(TypeName="decimal(10,2)")]
        [Display(Name="Manopera (lei)")]
        public decimal Manopera { get; set; }=0;

        [Column(TypeName="decimal(5,2)")]
        [Display(Name="Durata (ore)")]
        public decimal DurataOre { get; set; }=0;

       
        public Programare Programare { get; set; }=null!;
        public ICollection<LucrarePiesa> LucrarePiese { get; set; }=new List<LucrarePiesa>();
    }
}
