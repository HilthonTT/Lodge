using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "response",
                table: "idempotent_requests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_on_utc",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: bookings that are (or were) reserved, confirmed or completed were accepted. Cancelled bookings
            // count as accepted when they had been paid or the host cancelled them; a guest cancellation of an unpaid
            // booking cannot be told apart from a cancelled request, so it stays null (not counted).
            migrationBuilder.Sql(
                """
                UPDATE bookings
                SET accepted_on_utc = COALESCE(confirmed_on_utc, created_on_utc)
                WHERE status IN (1, 2, 5)
                   OR (status = 4 AND (confirmed_on_utc IS NOT NULL OR cancelled_by_host));
                """);

            // Review reply notifications linked to /stays/{id} (the checkout page); the listing page is /apartments/{id}.
            migrationBuilder.Sql(
                """
                UPDATE notifications
                SET link = regexp_replace(link, '^/stays/([0-9a-fA-F-]+)$', '/apartments/\1#reviews')
                WHERE type = 'ReviewReplyReceived' AND link LIKE '/stays/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "response",
                table: "idempotent_requests");

            migrationBuilder.DropColumn(
                name: "accepted_on_utc",
                table: "bookings");
        }
    }
}
