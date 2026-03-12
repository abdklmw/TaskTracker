using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AllowSameProductDifferentCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceExpenseID",
                table: "InvoiceExpenses",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses",
                column: "InvoiceExpenseID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceExpenses_InvoiceID_ProductID_UnitAmount",
                table: "InvoiceExpenses",
                columns: new[] { "InvoiceID", "ProductID", "UnitAmount" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceExpenses_InvoiceID_ProductID_UnitAmount",
                table: "InvoiceExpenses");

            migrationBuilder.DropColumn(
                name: "InvoiceExpenseID",
                table: "InvoiceExpenses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses",
                columns: new[] { "InvoiceID", "ProductID" });
        }
    }
}
