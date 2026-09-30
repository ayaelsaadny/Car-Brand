using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limousine.Infrastracture.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDescriptionAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vehicles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Vehicles");
        }
    }
}
