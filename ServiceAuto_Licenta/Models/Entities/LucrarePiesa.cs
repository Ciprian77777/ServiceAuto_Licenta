using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceAutoLicenta.Models.Entities
{
    public class LucrarePiesa
    {
        public int Id { get; set; }

        [Required]
        public int LucrareId { get; set; }

        [Required]
        public int PiesaId { get; set; }

        [Required]
        [Display(Name="Cantitate")]
        public int Cantitate { get; set; }=1;

        [Required, Column(TypeName="decimal(10,2)")]
        [Display(Name="Pret unitar")]
        public decimal PretUnitar { get; set; }

        // Proprietate calculată
        [NotMapped]
        public decimal Subtotal=>Cantitate * PretUnitar;

        public Lucrare Lucrare { get; set; }=null!;
        public Piesa Piesa { get; set; }=null!;
    }
}
