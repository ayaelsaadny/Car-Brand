using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limousine.Infrastracture.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSnapshotToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "BookingRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerFullName",
                table: "BookingRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneNumber",
                table: "BookingRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "CustomerFullName",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneNumber",
                table: "BookingRequests");
        }
    }
}
