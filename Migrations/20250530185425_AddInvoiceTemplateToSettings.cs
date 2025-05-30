using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTemplateToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceTemplate",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "InvoiceTemplate",
                value: "\r\n<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <style>\r\n        body { font-family: Helvetica, Arial, sans-serif; font-size: 12px; color: #333; }\r\n        .header { text-align: left; margin-bottom: 20px; }\r\n        .title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 20px; }\r\n        .client-info { margin-bottom: 20px; }\r\n        table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }\r\n        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }\r\n        th { background-color: #f4f4f4; font-weight: bold; }\r\n        .total { text-align: right; font-size: 14px; font-weight: bold; }\r\n        .footer { margin-top: 20px; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class='header'>\r\n        <p>{{CompanyName}}</p>\r\n        <p>{{AccountsReceivableAddress}}</p>\r\n        <p>{{AccountsReceivablePhone}}</p>\r\n        <p>{{AccountsReceivableEmail}}</p>\r\n    </div>\r\n    <div class='title'>Invoice #{{InvoiceID}}</div>\r\n    <div class='client-info'>\r\n        <p>Billed To: {{ClientName}}</p>\r\n        <p>Invoice Date: {{InvoiceDate}}</p>\r\n        <p>Total Amount: ${{TotalAmount}}</p>\r\n        <p>Status: {{Status}}</p>\r\n    </div>\r\n    {{TimeEntriesTable}}\r\n    {{ExpensesTable}}\r\n    <div class='total'>Total: ${{TotalAmount}}</div>\r\n    <div class='footer'>\r\n        <p>{{PaymentInformation}}</p>\r\n        <p>{{ThankYouMessage}}</p>\r\n    </div>\r\n</body>\r\n</html>");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceTemplate",
                table: "Settings");
        }
    }
}
