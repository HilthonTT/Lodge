using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Created_Idempotency_Requests_And_Modified_Apartment_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "maximum_guest_count",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "maximum_room_count",
                table: "apartments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "idempotent_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotent_requests", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotent_requests");

            migrationBuilder.DropColumn(
                name: "maximum_guest_count",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "maximum_room_count",
                table: "apartments");
        }
    }
}
