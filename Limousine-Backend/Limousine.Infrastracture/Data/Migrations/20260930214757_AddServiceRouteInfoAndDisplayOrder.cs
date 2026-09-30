using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limousine.Infrastracture.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRouteInfoAndDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Services",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "Services",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                table: "Services",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Services",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Services");
        }
    }
}
