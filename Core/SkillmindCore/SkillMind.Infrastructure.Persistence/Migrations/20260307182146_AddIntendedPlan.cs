using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntendedPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntendedPlan",
                table: "UserSubscription",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntendedPlan",
                table: "UserSubscription");
        }
    }
}
