using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpenseDate",
                table: "Expenses",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Expenses
                SET ExpenseDate = COALESCE(InvoicedDate, InvoiceSent, PaidDate)
                WHERE ExpenseDate IS NULL
                  AND (InvoicedDate IS NOT NULL OR InvoiceSent IS NOT NULL OR PaidDate IS NOT NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseDate",
                table: "Expenses");
        }
    }
}
