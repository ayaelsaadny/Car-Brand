using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limousine.Infrastracture.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAllowVehicleSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowVehicleSelection",
                table: "Services",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowVehicleSelection",
                table: "Services");
        }
    }
}
