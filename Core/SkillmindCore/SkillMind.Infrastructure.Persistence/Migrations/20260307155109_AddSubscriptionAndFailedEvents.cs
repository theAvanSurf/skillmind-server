using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndFailedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailedStripeEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalTopic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalPayload = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedStripeEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StripePriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StripeLookupKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscriptionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Plan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsInGracePeriod = table.Column<bool>(type: "boolean", nullable: false),
                    RetryAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastIdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscription", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedStripeEvent_FailedAt",
                table: "FailedStripeEvent",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FailedStripeEvent_IsResolved",
                table: "FailedStripeEvent",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscription_StripeSubscriptionId",
                table: "UserSubscription",
                column: "StripeSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscription_UserId",
                table: "UserSubscription",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedStripeEvent");

            migrationBuilder.DropTable(
                name: "UserSubscription");
        }
    }
}
