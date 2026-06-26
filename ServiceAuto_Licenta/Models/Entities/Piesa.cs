using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceAutoLicenta.Models.Entities
{
    public class Piesa
    {
        public int Id {get;set;}
        public string? UserId {get;set;}

        [Required, MaxLength(50)]
        [Display(Name="Cod piesă")]
        public string CodPiesa {get;set;}=string.Empty;

        [Required, MaxLength(200)]
        [Display(Name="Denumire")]
        public string Denumire {get;set;}=string.Empty;
        [MaxLength(100)]
        [Display(Name="Producator")]
        public string? Producator {get;set;}

        [Required, Column(TypeName="decimal(10,2)")]
        [Display(Name="Pret achizitie")]
        public decimal PretAchizitie {get;set;}
        [Required, Column(TypeName="decimal(10,2)")]
        [Display(Name="Pret vanzare")]
        public decimal PretVanzare {get;set;}

        [Display(Name="Stoc curent")]
        public int StocCurent {get;set;}=0;

        [Display(Name="Stoc minim")]
        public int StocMinim {get;set;}=1;

        public DateTime CreatedAt {get;set;}=DateTime.Now;

        [NotMapped]
        public bool StocScazut=>StocCurent<=StocMinim;

        public ICollection<LucrarePiesa> LucrarePiese {get;set;}=new List<LucrarePiesa>();
    }
}
