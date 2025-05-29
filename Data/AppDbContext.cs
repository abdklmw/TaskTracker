using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Models;

namespace TaskTracker.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceTimeEntry> InvoiceTimeEntries { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<InvoiceExpense> InvoiceExpenses { get; set; }
        public DbSet<Settings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TimeEntry relationships
            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.Client)
                .WithMany()
                .HasForeignKey(t => t.ClientID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(t => t.ProjectID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InvoiceTimeEntry composite key
            modelBuilder.Entity<InvoiceTimeEntry>()
                .HasKey(ite => new { ite.InvoiceID, ite.TimeEntryID });

            // Configure InvoiceProduct composite key
            modelBuilder.Entity<InvoiceExpense>()
                .HasKey(ip => new { ip.InvoiceID, ip.ProductID });

            // Invoice relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientID)
                .OnDelete(DeleteBehavior.Cascade);

            // InvoiceTimeEntry relationships
            modelBuilder.Entity<InvoiceTimeEntry>()
                .HasOne(ite => ite.Invoice)
                .WithMany(i => i.InvoiceTimeEntries)
                .HasForeignKey(ite => ite.InvoiceID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceTimeEntry>()
                .HasOne(ite => ite.TimeEntry)
                .WithMany()
                .HasForeignKey(ite => ite.TimeEntryID)
                .OnDelete(DeleteBehavior.NoAction);

            // InvoiceExpenses relationships
            modelBuilder.Entity<InvoiceExpense>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.InvoiceExpenses)
                .HasForeignKey(ip => ip.InvoiceID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceExpense>()
                .HasOne(ip => ip.Product)
                .WithMany(p => p.InvoiceProducts)
                .HasForeignKey(ip => ip.ProductID)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Settings entity
            // Seed a default Settings record
            modelBuilder.Entity<Settings>().HasData(
                new Settings
                {
                    Id = 1,
                    CompanyName = "Default Company",
                    AccountsReceivableAddress = null,
                    AccountsReceivablePhone = null,
                    AccountsReceivableEmail = null,
                    PaymentInformation = null,
                    ThankYouMessage = null,
                    SingletonGuard = 0
                }
            );

            // Enforce single record with a unique index
            modelBuilder.Entity<Settings>()
                .Property<int>("SingletonGuard")
                .HasDefaultValue(0)
                .ValueGeneratedNever();
            modelBuilder.Entity<Settings>()
                .HasIndex("SingletonGuard")
                .IsUnique();

            // Configure unique index for ProductSku
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductSku)
                .IsUnique();
        }
    }
}