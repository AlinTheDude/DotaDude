using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotaDude.Migrations
{
    public partial class AddLastLoginDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginDate",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 3, 31, 11, 47, 30, 358, DateTimeKind.Local).AddTicks(3422));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginDate",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 3, 31, 11, 41, 21, 697, DateTimeKind.Local).AddTicks(1321));
        }
    }
}
