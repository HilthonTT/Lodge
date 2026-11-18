using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HostingAndListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_apartment_id",
                table: "bookings");

            // Existing listings keep their room count as the number of bedrooms.
            migrationBuilder.RenameColumn(
                name: "maximum_room_count",
                table: "apartments",
                newName: "bedrooms");

            migrationBuilder.AddColumn<int>(
                name: "weekly_discount_percent",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "is_superhost",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "discount_currency",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "expired_on_utc",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "guest_count",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "message_to_host",
                table: "bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bathrooms",
                table: "apartments",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "beds",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "check_in_instructions",
                table: "apartments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "check_in_time",
                table: "apartments",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(15, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "check_out_time",
                table: "apartments",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(11, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "deleted",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_on_utc",
                table: "apartments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "house_rules",
                table: "apartments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "instant_book",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_listed",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "apartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "apartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "max_nights",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 365);

            migrationBuilder.AddColumn<int>(
                name: "min_nights",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "monthly_discount_percent",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Filled below, then made required.
            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "apartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "parties_allowed",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pets_allowed",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "property_type",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "smoking_allowed",
                table: "apartments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "weekend_price",
                table: "apartments",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "apartment_calendar_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_apartment_calendar_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_apartment_calendar_days_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "apartment_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_apartment_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_apartment_images_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlists", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                columns: table => new
                {
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_items", x => new { x.wishlist_id, x.apartment_id });
                    table.ForeignKey(
                        name: "fk_wishlist_items_wishlists_wishlist_id",
                        column: x => x.wishlist_id,
                        principalTable: "wishlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Existing data: listings without a host belong to the first admin (or else the oldest user) until the
            // demo seeder hands the demo listings to their hosts; every listing needs a host from now on.
            migrationBuilder.Sql(
                """
                UPDATE apartments
                SET owner_id = (
                    SELECT u.id FROM users AS u
                    ORDER BY (u.role = 'Admin') DESC, u.created_on_utc, u.id
                    LIMIT 1)
                WHERE owner_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                table: "apartments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // The single image of each existing listing becomes its cover photo.
            migrationBuilder.Sql(
                """
                INSERT INTO apartment_images (id, apartment_id, path, position)
                SELECT gen_random_uuid(), a.id, a.image_url, 0
                FROM apartments AS a
                WHERE a.image_url <> '';
                """);

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "apartments");

            // Beds default to the number of bedrooms; the removed amenities (11 CountrySide, 12 TinyHomes, 13 OMG)
            // are dropped; existing bookings get their currency on the (zero) discount.
            migrationBuilder.Sql(
                """
                UPDATE apartments SET beds = GREATEST(bedrooms, 1);
                UPDATE apartments SET amenities = array_remove(array_remove(array_remove(amenities, 11), 12), 13);
                UPDATE bookings SET discount_currency = total_price_currency;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_apartment_id_status",
                table: "bookings",
                columns: new[] { "apartment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_created_on_utc",
                table: "bookings",
                columns: new[] { "status", "created_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_apartments_amenities",
                table: "apartments",
                column: "amenities")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_apartments_latitude_longitude",
                table: "apartments",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_apartments_owner_id",
                table: "apartments",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_apartments_price_amount",
                table: "apartments",
                column: "price_amount");

            migrationBuilder.CreateIndex(
                name: "ix_apartment_calendar_days_apartment_id_date",
                table: "apartment_calendar_days",
                columns: new[] { "apartment_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_apartment_images_apartment_id_position",
                table: "apartment_images",
                columns: new[] { "apartment_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_items_apartment_id",
                table: "wishlist_items",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "ix_wishlists_user_id",
                table: "wishlists",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_apartments_users_owner_id",
                table: "apartments",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_apartments_users_owner_id",
                table: "apartments");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "apartments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE apartments AS a
                SET image_url = COALESCE(i.path, '/api/v1/files/' || i.id::text)
                FROM apartment_images AS i
                WHERE i.apartment_id = a.id AND i.position = 0;
                """);

            migrationBuilder.DropTable(
                name: "apartment_calendar_days");

            migrationBuilder.DropTable(
                name: "apartment_images");

            migrationBuilder.DropTable(
                name: "wishlist_items");

            migrationBuilder.DropTable(
                name: "wishlists");

            migrationBuilder.DropIndex(
                name: "ix_bookings_apartment_id_status",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_status_created_on_utc",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_apartments_amenities",
                table: "apartments");

            migrationBuilder.DropIndex(
                name: "ix_apartments_latitude_longitude",
                table: "apartments");

            migrationBuilder.DropIndex(
                name: "ix_apartments_owner_id",
                table: "apartments");

            migrationBuilder.DropIndex(
                name: "ix_apartments_price_amount",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "is_superhost",
                table: "users");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "discount_currency",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "expired_on_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "guest_count",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "message_to_host",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "bathrooms",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "beds",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "check_in_instructions",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "check_in_time",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "check_out_time",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "deleted",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "deleted_on_utc",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "house_rules",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "instant_book",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "is_listed",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "max_nights",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "min_nights",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "monthly_discount_percent",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "parties_allowed",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "pets_allowed",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "property_type",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "smoking_allowed",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "weekend_price",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "weekly_discount_percent",
                table: "apartments");

            migrationBuilder.RenameColumn(
                name: "bedrooms",
                table: "apartments",
                newName: "maximum_room_count");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_apartment_id",
                table: "bookings",
                column: "apartment_id");
        }
    }
}
