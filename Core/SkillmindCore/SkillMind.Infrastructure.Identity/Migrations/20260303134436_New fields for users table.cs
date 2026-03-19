using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class Newfieldsforuserstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                schema: "Identity",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                schema: "Identity",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "Identity",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                schema: "Identity",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
