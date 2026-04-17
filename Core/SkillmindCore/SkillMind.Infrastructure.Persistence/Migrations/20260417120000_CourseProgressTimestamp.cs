using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CourseProgressTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LastTimestampSeconds",
                table: "CourseProgress",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimestampSeconds",
                table: "CourseProgress");
        }
    }
}
