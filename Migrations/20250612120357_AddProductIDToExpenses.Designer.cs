﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskTracker.Data;

#nullable disable

namespace TaskTracker.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250612120357_AddProductIDToExpenses")]
    partial class AddProductIDToExpenses
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("TaskTracker.Data.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("TaskTracker.Models.Client.Client", b =>
                {
                    b.Property<int>("ClientID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ClientID"));

                    b.Property<string>("AccountsReceivableName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DefaultRate")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ClientID");

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("TaskTracker.Models.Expense.Expense", b =>
                {
                    b.Property<int>("ExpenseID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ExpenseID"));

                    b.Property<int>("ClientID")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("InvoiceSent")
                        .HasColumnType("date");

                    b.Property<DateTime?>("InvoicedDate")
                        .HasColumnType("date");

                    b.Property<DateTime?>("PaidDate")
                        .HasColumnType("date");

                    b.Property<int>("ProductID")
                        .HasColumnType("int");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("UnitAmount")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("ExpenseID");

                    b.HasIndex("ClientID");

                    b.HasIndex("ProductID");

                    b.ToTable("Expenses");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.Invoice", b =>
                {
                    b.Property<int>("InvoiceID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("InvoiceID"));

                    b.Property<int>("ClientID")
                        .HasColumnType("int");

                    b.Property<DateTime>("InvoiceDate")
                        .HasColumnType("date");

                    b.Property<DateTime?>("InvoiceSentDate")
                        .HasColumnType("date");

                    b.Property<DateTime?>("PaidDate")
                        .HasColumnType("date");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("InvoiceID");

                    b.HasIndex("ClientID");

                    b.ToTable("Invoices");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.InvoiceExpense", b =>
                {
                    b.Property<int>("InvoiceID")
                        .HasColumnType("int");

                    b.Property<int>("ProductID")
                        .HasColumnType("int");

                    b.Property<string>("AdditionalNotes")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsRecurring")
                        .HasColumnType("bit");

                    b.Property<DateOnly>("ProductInvoiceDate")
                        .HasColumnType("date");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("RecurringFrequency")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("InvoiceID", "ProductID");

                    b.HasIndex("ProductID");

                    b.ToTable("InvoiceExpenses");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.InvoiceTimeEntry", b =>
                {
                    b.Property<int>("InvoiceID")
                        .HasColumnType("int");

                    b.Property<int>("TimeEntryID")
                        .HasColumnType("int");

                    b.Property<string>("AdditionalNotes")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("InvoiceID", "TimeEntryID");

                    b.HasIndex("TimeEntryID");

                    b.ToTable("InvoiceTimeEntries");
                });

            modelBuilder.Entity("TaskTracker.Models.Product.Product", b =>
                {
                    b.Property<int>("ProductID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProductID"));

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ProductSku")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("ProductID");

                    b.HasIndex("ProductSku")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("TaskTracker.Models.Project.Project", b =>
                {
                    b.Property<int>("ProjectID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProjectID"));

                    b.Property<int?>("ClientID")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Rate")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("ProjectID");

                    b.HasIndex("ClientID");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("TaskTracker.Models.Settings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AccountsReceivableAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AccountsReceivableEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AccountsReceivablePhone")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("CompanyName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal>("DefaultHourlyRate")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("InvoiceTemplate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PaymentInformation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SingletonGuard")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("SmtpPassword")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int?>("SmtpPort")
                        .HasColumnType("int");

                    b.Property<string>("SmtpSenderEmail")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SmtpServer")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SmtpUsername")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ThankYouMessage")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SingletonGuard")
                        .IsUnique();

                    b.ToTable("Settings");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CompanyName = "Default Company",
                            DefaultHourlyRate = 0m,
                            InvoiceTemplate = "\r\n            <!DOCTYPE html>\r\n            <html>\r\n            <head>\r\n            <style>\r\n            body { font-family: Helvetica, Arial, sans-serif; font-size: 12px; color: #333; }\r\n            .header { overflow: auto; margin-bottom: 20px; }\r\n            .company-info { float: left; text-align: left; }\r\n            .payment-info { float: right; text-align: right; }\r\n            .title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 20px; clear: both; }\r\n            .company-name { font-size: 1.2em; font-weight: bold; }\r\n            .client-info { margin-bottom: 20px; }\r\n            table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }\r\n            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }\r\n            th { background-color: #f4f4f4; font-weight: bold; }\r\n            .total { text-align: right; font-size: 14px; font-weight: bold; }\r\n            .footer { margin-top: 20px; }\r\n            </style>\r\n            </head>\r\n            <body>\r\n            <div class='header'>\r\n            <div class='company-info'>\r\n            <p class='company-name'>{{CompanyName}}</p>\r\n            <p>{{AccountsReceivableAddress}}</p>\r\n            <p>{{AccountsReceivablePhone}}</p>\r\n            <p>{{AccountsReceivableEmail}}</p>\r\n            </div>\r\n            <div class='payment-info'>\r\n            <p>Payment Information</p>\r\n            <p>{{PaymentInformation}}</p>\r\n            </div>\r\n            </div>\r\n            <div class='title'>Invoice #{{InvoiceID}}</div>\r\n            <div class='client-info'>\r\n            <p>Billed To: {{ClientName}}</p>\r\n            <p>Invoice Date: {{InvoiceDate}}</p>\r\n            <p>Total Amount: ${{TotalAmount}}</p>\r\n            </div>\r\n            {{TimeEntriesTable}}\r\n            {{ExpensesTable}}\r\n            <div class='total'>Total: ${{TotalAmount}}</div>\r\n            <div class='footer'>\r\n            </div>\r\n            </body>\r\n            </html>",
                            SingletonGuard = 0
                        });
                });

            modelBuilder.Entity("TaskTracker.Models.TimeEntries.TimeEntry", b =>
                {
                    b.Property<int>("TimeEntryID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("TimeEntryID"));

                    b.Property<int>("ClientID")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EndDateTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("HourlyRate")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("HoursSpent")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("InvoiceSent")
                        .HasColumnType("date");

                    b.Property<DateTime?>("InvoicedDate")
                        .HasColumnType("date");

                    b.Property<DateTime?>("PaidDate")
                        .HasColumnType("date");

                    b.Property<int>("ProjectID")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("TimeEntryID");

                    b.HasIndex("ClientID");

                    b.HasIndex("ProjectID");

                    b.HasIndex("UserId");

                    b.ToTable("TimeEntries");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("TaskTracker.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("TaskTracker.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaskTracker.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("TaskTracker.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TaskTracker.Models.Expense.Expense", b =>
                {
                    b.HasOne("TaskTracker.Models.Client.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaskTracker.Models.Product.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.Invoice", b =>
                {
                    b.HasOne("TaskTracker.Models.Client.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.InvoiceExpense", b =>
                {
                    b.HasOne("TaskTracker.Models.Invoice.Invoice", "Invoice")
                        .WithMany("InvoiceExpenses")
                        .HasForeignKey("InvoiceID")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("TaskTracker.Models.Product.Product", "Product")
                        .WithMany("InvoiceProducts")
                        .HasForeignKey("ProductID")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Invoice");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.InvoiceTimeEntry", b =>
                {
                    b.HasOne("TaskTracker.Models.Invoice.Invoice", "Invoice")
                        .WithMany("InvoiceTimeEntries")
                        .HasForeignKey("InvoiceID")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("TaskTracker.Models.TimeEntries.TimeEntry", "TimeEntry")
                        .WithMany()
                        .HasForeignKey("TimeEntryID")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Invoice");

                    b.Navigation("TimeEntry");
                });

            modelBuilder.Entity("TaskTracker.Models.Project.Project", b =>
                {
                    b.HasOne("TaskTracker.Models.Client.Client", null)
                        .WithMany("Projects")
                        .HasForeignKey("ClientID");
                });

            modelBuilder.Entity("TaskTracker.Models.TimeEntries.TimeEntry", b =>
                {
                    b.HasOne("TaskTracker.Models.Client.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaskTracker.Models.Project.Project", "Project")
                        .WithMany("TimeEntries")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaskTracker.Data.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Client");

                    b.Navigation("Project");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TaskTracker.Models.Client.Client", b =>
                {
                    b.Navigation("Projects");
                });

            modelBuilder.Entity("TaskTracker.Models.Invoice.Invoice", b =>
                {
                    b.Navigation("InvoiceExpenses");

                    b.Navigation("InvoiceTimeEntries");
                });

            modelBuilder.Entity("TaskTracker.Models.Product.Product", b =>
                {
                    b.Navigation("InvoiceProducts");
                });

            modelBuilder.Entity("TaskTracker.Models.Project.Project", b =>
                {
                    b.Navigation("TimeEntries");
                });
#pragma warning restore 612, 618
        }
    }
}
