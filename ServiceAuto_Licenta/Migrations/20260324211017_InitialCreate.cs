using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceAutoLicenta.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nume = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenume = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Adresa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cnp = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Clienti", x=>x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Piese",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodPiesa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Denumire = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Producator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PretAchizitie = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PretVanzare = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StocCurent = table.Column<int>(type: "int", nullable: false),
                    StocMinim = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Piese", x=>x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Masini",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AnFabricatie = table.Column<int>(type: "int", nullable: true),
                    NrInmatriculare = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Vin = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    KmActuali = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Masini", x=>x.Id);
                    table.ForeignKey(
                        name: "FK_Masini_Clienti_ClientId",
                        column: x=>x.ClientId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Programari",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasinaId = table.Column<int>(type: "int", nullable: false),
                    DataIntrare = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataIesire = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observatii = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalFaraTva = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tva = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalCuTva = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Programari", x=>x.Id);
                    table.ForeignKey(
                        name: "FK_Programari_Masini_MasinaId",
                        column: x=>x.MasinaId,
                        principalTable: "Masini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facturi",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramareId = table.Column<int>(type: "int", nullable: false),
                    SerieNumar = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataEmitere = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataScadenta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusPlata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetodaPlata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TvaValoare = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Facturi", x=>x.Id);
                    table.ForeignKey(
                        name: "FK_Facturi_Programari_ProgramareId",
                        column: x=>x.ProgramareId,
                        principalTable: "Programari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lucrari",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramareId = table.Column<int>(type: "int", nullable: false),
                    Denumire = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descriere = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manopera = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DurataOre = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_Lucrari", x=>x.Id);
                    table.ForeignKey(
                        name: "FK_Lucrari_Programari_ProgramareId",
                        column: x=>x.ProgramareId,
                        principalTable: "Programari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LucrarePiese",
                columns: table=>new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LucrareId = table.Column<int>(type: "int", nullable: false),
                    PiesaId = table.Column<int>(type: "int", nullable: false),
                    Cantitate = table.Column<int>(type: "int", nullable: false),
                    PretUnitar = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table=>
                {
                    table.PrimaryKey("PK_LucrarePiese", x=>x.Id);
                    table.ForeignKey(
                        name: "FK_LucrarePiese_Lucrari_LucrareId",
                        column: x=>x.LucrareId,
                        principalTable: "Lucrari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LucrarePiese_Piese_PiesaId",
                        column: x=>x.PiesaId,
                        principalTable: "Piese",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Clienti",
                columns: new[] { "Id", "Adresa", "Cnp", "CreatedAt", "Email", "Nume", "Prenume", "Telefon" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1641), "ion.banea@email.ro", "Banea", "Ion", "0722111222" },
                    { 2, null, null, new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1696), "cristi.nicula1@email.ro", "Nicula", "Cristian", "0733222333" }
                });

            migrationBuilder.InsertData(
                table: "Piese",
                columns: new[] { "Id", "CodPiesa", "CreatedAt", "Denumire", "PretAchizitie", "PretVanzare", "Producator", "StocCurent", "StocMinim" },
                values: new object[,]
                {
                    { 1, "FIL-001", new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1903), "Filtru ulei Dacia Logan", 18.50m, 35.00m, "Mann Filter", 10, 3 },
                    { 2, "PLQ-002", new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1913), "Set placute frana fata VW Golf", 65.00m, 120.00m, "Bosch", 4, 2 },
                    { 3, "ULI-003", new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1918), "Ulei motor 5W40 4L", 55.00m, 89.00m, "Castrol", 15, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_Cnp",
                table: "Clienti",
                column: "Cnp",
                unique: true,
                filter: "[Cnp] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Facturi_ProgramareId",
                table: "Facturi",
                column: "ProgramareId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturi_SerieNumar",
                table: "Facturi",
                column: "SerieNumar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LucrarePiese_LucrareId",
                table: "LucrarePiese",
                column: "LucrareId");

            migrationBuilder.CreateIndex(
                name: "IX_LucrarePiese_PiesaId",
                table: "LucrarePiese",
                column: "PiesaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lucrari_ProgramareId",
                table: "Lucrari",
                column: "ProgramareId");

            migrationBuilder.CreateIndex(
                name: "IX_Masini_ClientId",
                table: "Masini",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Masini_NrInmatriculare",
                table: "Masini",
                column: "NrInmatriculare",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Masini_Vin",
                table: "Masini",
                column: "Vin",
                unique: true,
                filter: "[Vin] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Piese_CodPiesa",
                table: "Piese",
                column: "CodPiesa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programari_MasinaId",
                table: "Programari",
                column: "MasinaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Facturi");

            migrationBuilder.DropTable(
                name: "LucrarePiese");

            migrationBuilder.DropTable(
                name: "Lucrari");

            migrationBuilder.DropTable(
                name: "Piese");

            migrationBuilder.DropTable(
                name: "Programari");

            migrationBuilder.DropTable(
                name: "Masini");

            migrationBuilder.DropTable(
                name: "Clienti");
        }
    }
}
