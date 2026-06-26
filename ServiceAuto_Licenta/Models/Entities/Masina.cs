using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceAutoLicenta.Models.Entities

{
    public class Masina
    {
        public int Id {get;set;}

        [Required]
        public int ClientId {get;set;}

        [Required, MaxLength(60)]
        [Display(Name="Marca")]
        public string Marca {get;set;}=string.Empty;
        [Required, MaxLength(60)]
        [Display(Name="Model")]
        public string ModelMasina {get;set;}=string.Empty;

        [Display(Name="An fabricatie")]
        public int? AnFabricatie {get;set;}

        [Required, MaxLength(10)]
        [Display(Name="Nr. înmatriculare")]
        public string NrInmatriculare {get;set;}=string.Empty;

        [MaxLength(17)]
        [Display(Name="VIN")]
        public string? Vin {get;set;}
        [Display(Name="Km actuali")]
        public int? KmActuali {get;set;} 

        public DateTime CreatedAt {get;set;}=DateTime.Now;
        public Client Client {get;set;}=null!;
        public ICollection<Programare> Programari {get;set;}=new List<Programare>();
    }
}
