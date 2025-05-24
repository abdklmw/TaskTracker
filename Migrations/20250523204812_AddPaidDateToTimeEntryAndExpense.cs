using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidDateToTimeEntryAndExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidDate",
                table: "TimeEntries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicedDate",
                table: "Expenses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidDate",
                table: "Expenses",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "InvoicedDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "Expenses");
        }
    }
}
