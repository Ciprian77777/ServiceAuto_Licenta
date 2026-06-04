using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceAutoLicenta.Models.Entities
{
    public enum StatusPlata { Neplata, Platita, Partial }
    public enum MetodaPlata { Numerar, Card, Transfer, Altele }
    public class Factura
    {
        public int Id { get; set; }

        [Required]
        public int ProgramareId { get; set; }

        [Required, MaxLength(20)]
        [Display(Name="Serie/Numar")]
        public string SerieNumar { get; set; }=string.Empty;

        [Required]
        [Display(Name="Data emitere")]
        public DateTime DataEmitere { get; set; }

        [Display(Name="Data scadenta")]
        public DateTime? DataScadenta { get; set; }

        [Display(Name="Status plata")]
        public StatusPlata StatusPlata { get; set; }=StatusPlata.Neplata;

        [Display(Name="Metoda plata")]
        public MetodaPlata? MetodaPlata { get; set; }

        [Column(TypeName="decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName="decimal(10,2)")]
        public decimal TvaValoare { get; set; }

        [Column(TypeName="decimal(10,2)")]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }=DateTime.Now;

        public Programare Programare { get; set; }=null!;
    }
}
