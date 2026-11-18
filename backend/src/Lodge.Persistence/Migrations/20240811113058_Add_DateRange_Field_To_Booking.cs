using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_DateRange_Field_To_Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "duration_end",
                table: "bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "duration_start",
                table: "bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duration_end",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "duration_start",
                table: "bookings");
        }
    }
}
