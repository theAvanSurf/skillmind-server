using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CertificateTemplateBodyHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateTemplate_Course_CourseId",
                table: "CertificateTemplate");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "CertificateTemplate",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml",
                table: "CertificateTemplate",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "CertificateTemplate",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateTemplate_Course_CourseId",
                table: "CertificateTemplate",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateTemplate_Course_CourseId",
                table: "CertificateTemplate");

            migrationBuilder.DropColumn(
                name: "BodyHtml",
                table: "CertificateTemplate");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "CertificateTemplate");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "CertificateTemplate",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateTemplate_Course_CourseId",
                table: "CertificateTemplate",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
