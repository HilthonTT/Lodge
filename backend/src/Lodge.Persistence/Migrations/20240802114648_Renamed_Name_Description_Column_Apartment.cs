using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Renamed_Name_Description_Column_Apartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name_value",
                table: "apartments",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "description_value",
                table: "apartments",
                newName: "description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "apartments",
                newName: "name_value");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "apartments",
                newName: "description_value");
        }
    }
}
