using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeStripeFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the unique index first (allows duplicate empty strings during transition)
            migrationBuilder.DropIndex(
                name: "IX_UserSubscription_StripeSubscriptionId",
                table: "UserSubscription");

            // 2. Drop NOT NULL constraints before we can set values to NULL
            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "StripePriceId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "StripeLookupKey",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            // 3. Convert existing empty strings to NULL (safe now that NOT NULL is dropped)
            migrationBuilder.Sql("UPDATE \"UserSubscription\" SET \"StripeSubscriptionId\" = NULL WHERE \"StripeSubscriptionId\" = ''");
            migrationBuilder.Sql("UPDATE \"UserSubscription\" SET \"StripeCustomerId\" = NULL WHERE \"StripeCustomerId\" = ''");
            migrationBuilder.Sql("UPDATE \"UserSubscription\" SET \"StripePriceId\" = NULL WHERE \"StripePriceId\" = ''");
            migrationBuilder.Sql("UPDATE \"UserSubscription\" SET \"StripeLookupKey\" = NULL WHERE \"StripeLookupKey\" = ''");

            // 4. Recreate as a partial unique index — NULL rows are excluded, so multiple free users are fine
            migrationBuilder.CreateIndex(
                name: "IX_UserSubscription_StripeSubscriptionId",
                table: "UserSubscription",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "\"StripeSubscriptionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscription_StripeSubscriptionId",
                table: "UserSubscription");

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripePriceId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripeLookupKey",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "UserSubscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscription_StripeSubscriptionId",
                table: "UserSubscription",
                column: "StripeSubscriptionId",
                unique: true);
        }
    }
}
