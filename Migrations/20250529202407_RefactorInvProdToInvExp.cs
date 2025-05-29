using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInvProdToInvExp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_Invoices_InvoiceID",
                table: "InvoiceProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceProducts_Products_ProductID",
                table: "InvoiceProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceProducts",
                table: "InvoiceProducts");

            migrationBuilder.RenameTable(
                name: "InvoiceProducts",
                newName: "InvoiceExpenses");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceProducts_ProductID",
                table: "InvoiceExpenses",
                newName: "IX_InvoiceExpenses_ProductID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses",
                columns: new[] { "InvoiceID", "ProductID" });

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceExpenses_Invoices_InvoiceID",
                table: "InvoiceExpenses",
                column: "InvoiceID",
                principalTable: "Invoices",
                principalColumn: "InvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceExpenses_Products_ProductID",
                table: "InvoiceExpenses",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceExpenses_Invoices_InvoiceID",
                table: "InvoiceExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceExpenses_Products_ProductID",
                table: "InvoiceExpenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceExpenses",
                table: "InvoiceExpenses");

            migrationBuilder.RenameTable(
                name: "InvoiceExpenses",
                newName: "InvoiceProducts");

            migrationBuilder.RenameIndex(
                name: "IX_InvoiceExpenses_ProductID",
                table: "InvoiceProducts",
                newName: "IX_InvoiceProducts_ProductID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceProducts",
                table: "InvoiceProducts",
                columns: new[] { "InvoiceID", "ProductID" });

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_Invoices_InvoiceID",
                table: "InvoiceProducts",
                column: "InvoiceID",
                principalTable: "Invoices",
                principalColumn: "InvoiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceProducts_Products_ProductID",
                table: "InvoiceProducts",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID");
        }
    }
}
