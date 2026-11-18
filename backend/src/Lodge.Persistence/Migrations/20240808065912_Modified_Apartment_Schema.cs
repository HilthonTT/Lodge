using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Modified_Apartment_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_id",
                table: "apartments");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "apartments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "apartments");

            migrationBuilder.AddColumn<Guid>(
                name: "image_id",
                table: "apartments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
