using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceAutoLicenta.Migrations
{
    /// <inheritdoc />
    public partial class RenameModelToModelMasina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Model",
                table: "Masini",
                newName: "ModelMasina");

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 25, 0, 17, 18, 538, DateTimeKind.Local).AddTicks(8231));

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 25, 0, 17, 18, 538, DateTimeKind.Local).AddTicks(8290));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 25, 0, 17, 18, 538, DateTimeKind.Local).AddTicks(8501));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 25, 0, 17, 18, 538, DateTimeKind.Local).AddTicks(8515));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 25, 0, 17, 18, 538, DateTimeKind.Local).AddTicks(8520));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModelMasina",
                table: "Masini",
                newName: "Model");

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1641));

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1696));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1903));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1913));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 23, 10, 16, 278, DateTimeKind.Local).AddTicks(1918));
        }
    }
}
