using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "Settings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "Settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpSenderEmail",
                table: "Settings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpServer",
                table: "Settings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "Settings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "SmtpPassword", "SmtpPort", "SmtpSenderEmail", "SmtpServer", "SmtpUsername" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SmtpSenderEmail",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SmtpServer",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "Settings");
        }
    }
}
