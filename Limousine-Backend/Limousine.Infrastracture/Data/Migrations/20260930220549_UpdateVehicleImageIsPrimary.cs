using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limousine.Infrastracture.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleImageIsPrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleImages_VehicleId",
                table: "VehicleImages");

            migrationBuilder.RenameColumn(
                name: "IsMain",
                table: "VehicleImages",
                newName: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleImages_VehicleId_IsPrimary",
                table: "VehicleImages",
                columns: new[] { "VehicleId", "IsPrimary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleImages_VehicleId_IsPrimary",
                table: "VehicleImages");

            migrationBuilder.RenameColumn(
                name: "IsPrimary",
                table: "VehicleImages",
                newName: "IsMain");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleImages_VehicleId",
                table: "VehicleImages",
                column: "VehicleId");
        }
    }
}
