using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MessagingNotificationsExperiences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payouts_bookings_booking_id",
                table: "payouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "booking_id",
                table: "payouts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "experience_booking_id",
                table: "payouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_message_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guest_last_read_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    host_last_read_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guest_last_emailed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    host_last_emailed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversations_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_users_guest_id",
                        column: x => x.guest_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_users_host_id",
                        column: x => x.host_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    meeting_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    price_per_guest = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    max_guests = table.Column<int>(type: "integer", nullable: false),
                    languages = table.Column<List<string>>(type: "text[]", nullable: false),
                    included = table.Column<List<string>>(type: "text[]", nullable: false),
                    image_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    is_listed = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experiences", x => x.id);
                    table.ForeignKey(
                        name: "fk_experiences_users_host_id",
                        column: x => x.host_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_messages = table.Column<bool>(type: "boolean", nullable: false),
                    email_reservations = table.Column<bool>(type: "boolean", nullable: false),
                    email_reviews = table.Column<bool>(type: "boolean", nullable: false),
                    email_payouts = table.Column<bool>(type: "boolean", nullable: false),
                    email_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_messages = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_reservations = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_reviews = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_payouts = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dedupe_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    in_app = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    system_event = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    booked_guests = table.Column<int>(type: "integer", nullable: false),
                    cancelled_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_sessions_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    price_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    service_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    amount_paid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    paid_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    refund_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    refund_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refunded_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_host = table.Column<bool>(type: "boolean", nullable: false),
                    completed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_bookings_experience_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "experience_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_experience_bookings_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_experience_bookings_users_guest_id",
                        column: x => x.guest_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_reviews_experience_bookings_experience_booking_id",
                        column: x => x.experience_booking_id,
                        principalTable: "experience_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_experience_reviews_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_experience_reviews_users_guest_id",
                        column: x => x.guest_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payouts_experience_booking_id",
                table: "payouts",
                column: "experience_booking_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_payouts_one_source",
                table: "payouts",
                sql: "(booking_id IS NULL) <> (experience_booking_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_apartment_id_guest_id",
                table: "conversations",
                columns: new[] { "apartment_id", "guest_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_guest_id_last_message_on_utc",
                table: "conversations",
                columns: new[] { "guest_id", "last_message_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_host_id_last_message_on_utc",
                table: "conversations",
                columns: new[] { "host_id", "last_message_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_bookings_experience_id",
                table: "experience_bookings",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "ix_experience_bookings_guest_id",
                table: "experience_bookings",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_experience_bookings_session_id",
                table: "experience_bookings",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_experience_bookings_status_session_starts_at_utc",
                table: "experience_bookings",
                columns: new[] { "status", "session_starts_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_reviews_experience_booking_id",
                table: "experience_reviews",
                column: "experience_booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experience_reviews_experience_id_created_on_utc",
                table: "experience_reviews",
                columns: new[] { "experience_id", "created_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_reviews_guest_id",
                table: "experience_reviews",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_experience_sessions_experience_id_starts_at_utc",
                table: "experience_sessions",
                columns: new[] { "experience_id", "starts_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_sessions_starts_at_utc",
                table: "experience_sessions",
                column: "starts_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_experiences_host_id",
                table: "experiences",
                column: "host_id");

            migrationBuilder.CreateIndex(
                name: "ix_experiences_is_listed_category",
                table: "experiences",
                columns: new[] { "is_listed", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id_sent_on_utc_id",
                table: "messages",
                columns: new[] { "conversation_id", "sent_on_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_id",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_on_utc",
                table: "notifications",
                columns: new[] { "user_id", "created_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_dedupe_key",
                table: "notifications",
                columns: new[] { "user_id", "dedupe_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payouts_bookings_booking_id",
                table: "payouts",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_payouts_experience_bookings_experience_booking_id",
                table: "payouts",
                column: "experience_booking_id",
                principalTable: "experience_bookings",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Experience payouts cannot exist without the experience tables.
            migrationBuilder.Sql("DELETE FROM payouts WHERE booking_id IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "fk_payouts_bookings_booking_id",
                table: "payouts");

            migrationBuilder.DropForeignKey(
                name: "fk_payouts_experience_bookings_experience_booking_id",
                table: "payouts");

            migrationBuilder.DropTable(
                name: "experience_reviews");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "experience_bookings");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "experience_sessions");

            migrationBuilder.DropTable(
                name: "experiences");

            migrationBuilder.DropIndex(
                name: "ix_payouts_experience_booking_id",
                table: "payouts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payouts_one_source",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "experience_booking_id",
                table: "payouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "booking_id",
                table: "payouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payouts_bookings_booking_id",
                table: "payouts",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
