using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotaDude.Migrations
{
    public partial class AddPreferredRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredRole",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "carry");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PreferredRole", "RegistrationDate" },
                values: new object[] { "carry", new DateTime(2025, 3, 31, 11, 41, 21, 697, DateTimeKind.Local).AddTicks(1321) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredRole",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 3, 31, 11, 7, 35, 813, DateTimeKind.Local).AddTicks(8922));
        }
    }
}
