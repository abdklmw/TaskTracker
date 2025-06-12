using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIDToExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {           
            // Insert default product
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, ProductSku, Description, UnitPrice)
                VALUES ('General Expense', 'EXP-GEN', 'Generic expense item', 0.00);
            ");

            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Update existing expenses
            migrationBuilder.Sql(@"
                UPDATE Expenses
                SET ProductID = (SELECT TOP 1 ProductID FROM Products WHERE ProductSku = 'EXP-GEN')
                WHERE ProductID = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ProductID",
                table: "Expenses",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Products_ProductID",
                table: "Expenses",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Products_ProductID",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ProductID",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "Expenses");
        }
    }
}
