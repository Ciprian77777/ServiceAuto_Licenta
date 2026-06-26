using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceAutoLicenta.Models.Entities
{ 
    public enum StatusProgramare
    {
        Programata,
        InLucru,
        Finalizata,
        Anulata
    }

    public class Programare
    {
        public int Id {get;set;}

        [Required]
        public int MasinaId {get;set;}

        [Required]
        [Display(Name="Data intrare")]
        public DateTime DataIntrare {get;set;}

        [Display(Name="Data iesire")]
        public DateTime? DataIesire {get;set;}
        [Display(Name="Status")]
        public StatusProgramare Status {get;set;}=StatusProgramare.Programata;

        [Display(Name="Observatii")]
        public string? Observatii {get;set;}

        [Column(TypeName="decimal(10,2)")]
        public decimal TotalFaraTva {get;set;}=0;

        [Column(TypeName="decimal(10,2)")]
        public decimal Tva {get;set;}=0;

        [Column(TypeName="decimal(10,2)")]
        public decimal TotalCuTva {get;set;}=0;

        public DateTime CreatedAt {get;set;}=DateTime.Now;

        public Masina Masina {get;set;}=null!;
        public ICollection<Lucrare> Lucrari {get;set;}=new List<Lucrare>();
        public Factura? Factura {get;set;}
    }
}

