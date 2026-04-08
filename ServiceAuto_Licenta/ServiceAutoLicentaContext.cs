using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Models.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ServiceAutoLicenta.Data
{
    public class ServiceAutoLicentaContext : DbContext
    {
        public ServiceAutoLicentaContext(DbContextOptions<ServiceAutoLicentaContext> options)
            : base(options) { }

        public DbSet<Client> Clienti { get; set; }
        public DbSet<Masina> Masini { get; set; }
        public DbSet<Programare> Programari { get; set; }
        public DbSet<Lucrare> Lucrari { get; set; }
        public DbSet<Piesa> Piese { get; set; }
        public DbSet<LucrarePiesa> LucrarePiese { get; set; }
        public DbSet<Factura> Facturi { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Cnp).IsUnique();

            modelBuilder.Entity<Masina>()
                .HasIndex(m => m.NrInmatriculare).IsUnique();

            modelBuilder.Entity<Masina>()
                .HasIndex(m => m.Vin).IsUnique();

            modelBuilder.Entity<Piesa>()
                .HasIndex(p => p.CodPiesa).IsUnique();

            modelBuilder.Entity<Factura>()
                .HasIndex(f => f.SerieNumar).IsUnique();

            // O programare are o singură factură
            modelBuilder.Entity<Factura>()
                .HasIndex(f => f.ProgramareId).IsUnique();

            // Enum-uri stocate ca string (mai lizibil în DB)
            modelBuilder.Entity<Programare>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Factura>()
                .Property(f => f.StatusPlata)
                .HasConversion<string>();

            modelBuilder.Entity<Factura>()
                .Property(f => f.MetodaPlata)
                .HasConversion<string>();

            modelBuilder.Entity<Client>().HasData(
                new Client { Id = 1, Nume = "Banea", Prenume = "Ion", Telefon = "0722111222", Email = "ion.banea@email.ro" },
                new Client { Id = 2, Nume = "Nicula", Prenume = "Cristian", Telefon = "0733222333", Email = "cristi.nicula1@email.ro" }
            );

            modelBuilder.Entity<Piesa>().HasData(
                new Piesa { Id = 1, CodPiesa = "FIL-001", Denumire = "Filtru ulei Dacia Logan", Producator = "Mann Filter", PretAchizitie = 18.50m, PretVanzare = 35.00m, StocCurent = 10, StocMinim = 3 },
                new Piesa { Id = 2, CodPiesa = "PLQ-002", Denumire = "Set placute frana fata VW Golf", Producator = "Bosch", PretAchizitie = 65.00m, PretVanzare = 120.00m, StocCurent = 4, StocMinim = 2 },
                new Piesa { Id = 3, CodPiesa = "ULI-003", Denumire = "Ulei motor 5W40 4L", Producator = "Castrol", PretAchizitie = 55.00m, PretVanzare = 89.00m, StocCurent = 15, StocMinim = 5 }
            );
        }
    }
}