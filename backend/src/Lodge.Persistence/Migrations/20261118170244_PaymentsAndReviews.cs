using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reviews_apartment_id",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_reviews_booking_id",
                table: "reviews");

            migrationBuilder.AddColumn<int>(
                name: "accuracy_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "check_in_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cleanliness_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "communication_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "host_reply",
                table: "reviews",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "host_reply_created_on_utc",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "host_reply_modified_on_utc",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "location_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_on_utc",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "value_rating",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "bookings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_policy",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Moderate");

            migrationBuilder.AddColumn<bool>(
                name: "cancelled_by_host",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_on_utc",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_intent_id",
                table: "bookings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "bookings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_id",
                table: "bookings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_status",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateTime>(
                name: "refunded_on_utc",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "service_fee_amount",
                table: "bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "service_fee_currency",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate_percent",
                table: "bookings",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxes_amount",
                table: "bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "taxes_currency",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_policy",
                table: "apartments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Moderate");

            // Existing data: fees snapshots take the booking currency; reviews written before categories and double-blind
            // publishing existed get their overall rating for every category and are published.
            migrationBuilder.Sql(
                """
                UPDATE bookings SET service_fee_currency = total_price_currency, taxes_currency = total_price_currency;

                UPDATE reviews
                SET cleanliness_rating = rating,
                    accuracy_rating = rating,
                    check_in_rating = rating,
                    communication_rating = rating,
                    location_rating = rating,
                    value_rating = rating,
                    is_published = TRUE,
                    published_on_utc = created_on_utc;
                """);

            migrationBuilder.CreateTable(
                name: "guest_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_reviews_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guest_reviews_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guest_reviews_users_guest_id",
                        column: x => x.guest_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guest_reviews_users_host_id",
                        column: x => x.host_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payout_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payouts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_payout_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_currency = table.Column<string>(type: "text", nullable: false),
                    host_fee_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheduled_for = table.Column<DateOnly>(type: "date", nullable: false),
                    paid_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transfer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_payouts_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payouts_users_host_id",
                        column: x => x.host_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_apartment_id_is_published",
                table: "reviews",
                columns: new[] { "apartment_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_booking_id",
                table: "reviews",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_reviews_apartment_id",
                table: "guest_reviews",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_reviews_booking_id",
                table: "guest_reviews",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_reviews_guest_id_is_published",
                table: "guest_reviews",
                columns: new[] { "guest_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_reviews_host_id",
                table: "guest_reviews",
                column: "host_id");

            migrationBuilder.CreateIndex(
                name: "ix_payout_accounts_external_account_id",
                table: "payout_accounts",
                column: "external_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payout_accounts_user_id",
                table: "payout_accounts",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payouts_booking_id",
                table: "payouts",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payouts_host_id",
                table: "payouts",
                column: "host_id");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_status_scheduled_for",
                table: "payouts",
                columns: new[] { "status", "scheduled_for" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guest_reviews");

            migrationBuilder.DropTable(
                name: "payout_accounts");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropIndex(
                name: "ix_reviews_apartment_id_is_published",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_reviews_booking_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "accuracy_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "check_in_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "cleanliness_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "communication_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "host_reply",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "host_reply_created_on_utc",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "host_reply_modified_on_utc",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_published",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "location_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "published_on_utc",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "value_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "amount_paid",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancellation_policy",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancelled_by_host",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "paid_on_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "payment_intent_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refunded_on_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "service_fee_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "service_fee_currency",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "tax_rate_percent",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "taxes_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "taxes_currency",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancellation_policy",
                table: "apartments");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_apartment_id",
                table: "reviews",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_booking_id",
                table: "reviews",
                column: "booking_id");
        }
    }
}
