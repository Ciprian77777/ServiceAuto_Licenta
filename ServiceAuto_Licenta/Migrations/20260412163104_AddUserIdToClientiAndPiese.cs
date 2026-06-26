using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceAutoLicenta.Migrations
{
    public partial class AddUserIdToClientiAndPiese : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Piese",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Clienti",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UserId" },
                values: new object[] { new DateTime(2026, 4, 12, 19, 31, 2, 829, DateTimeKind.Local).AddTicks(6111), "" });

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UserId" },
                values: new object[] { new DateTime(2026, 4, 12, 19, 31, 2, 829, DateTimeKind.Local).AddTicks(6168), "" });

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UserId" },
                values: new object[] { new DateTime(2026, 4, 12, 19, 31, 2, 829, DateTimeKind.Local).AddTicks(6411), "" });

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UserId" },
                values: new object[] { new DateTime(2026, 4, 12, 19, 31, 2, 829, DateTimeKind.Local).AddTicks(6428), "" });

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UserId" },
                values: new object[] { new DateTime(2026, 4, 12, 19, 31, 2, 829, DateTimeKind.Local).AddTicks(6434), "" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Piese");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Clienti");

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 9, 14, 5, 18, 631, DateTimeKind.Local).AddTicks(6754));

            migrationBuilder.UpdateData(
                table: "Clienti",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 9, 14, 5, 18, 631, DateTimeKind.Local).AddTicks(6827));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 9, 14, 5, 18, 631, DateTimeKind.Local).AddTicks(7168));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 9, 14, 5, 18, 631, DateTimeKind.Local).AddTicks(7189));

            migrationBuilder.UpdateData(
                table: "Piese",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 9, 14, 5, 18, 631, DateTimeKind.Local).AddTicks(7195));
        }
    }
}
