using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpensesRecordLimit",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesRecordLimit",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "PreferredClientId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeEntriesRecordLimit",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpensesRecordLimit",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoicesRecordLimit",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredClientId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TimeEntriesRecordLimit",
                table: "AspNetUsers");
        }
    }
}
