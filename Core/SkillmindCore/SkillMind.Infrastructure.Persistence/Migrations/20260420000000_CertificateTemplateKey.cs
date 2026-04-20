using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations;

public partial class CertificateTemplateKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TemplateKey",
            table: "CertificateTemplate",
            type: "text",
            nullable: false,
            defaultValue: "classic");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TemplateKey",
            table: "CertificateTemplate");
    }
}
