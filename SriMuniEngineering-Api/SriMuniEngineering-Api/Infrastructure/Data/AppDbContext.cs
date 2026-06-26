using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<JobWorkLedger> JobWorkLedgers => Set<JobWorkLedger>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── User ─────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
        });

        // ─── Customer ─────────────────────────────────────────
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GSTIN).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BillingAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Pincode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.GSTIN).HasMaxLength(20).IsRequired();
            entity.Property(e => e.StateName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.VendorCode).HasMaxLength(50);
        });

        // ─── Product ──────────────────────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PartNo).IsUnique();
            entity.Property(e => e.PartNo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PartName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PartDescription).HasMaxLength(500);
            entity.Property(e => e.BasePricePerUnit).HasColumnType("decimal(18,4)");
            entity.Property(e => e.HsnSac).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
        });

        // ─── JobWorkLedger ────────────────────────────────────
        modelBuilder.Entity<JobWorkLedger>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DcNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.JobWorkLedgers)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.JobWorkLedgers)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Quotation ───────────────────────────────────────
        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QuotationNo).IsUnique();
            entity.Property(e => e.QuotationNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.OperationsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.OtherCostsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcessCostTotal).HasColumnType("decimal(18,4)");
            entity.Property(e => e.EstimatedCostPerPart).HasColumnType("decimal(18,4)");
            entity.Property(e => e.GstRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.StoredFilePath).HasMaxLength(500);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Quotations)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Quotations)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Invoice ─────────────────────────────────────────
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNo).IsUnique();
            entity.Property(e => e.InvoiceNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Rate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TaxableValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IgstRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IgstAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CgstRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.CgstAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SgstRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.SgstAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountInWords).HasMaxLength(500);
            entity.Property(e => e.DeliveryNoteNo).HasMaxLength(100);
            entity.Property(e => e.ReferenceNo).HasMaxLength(100);
            entity.Property(e => e.BuyersOrderNo).HasMaxLength(100);
            entity.Property(e => e.DispatchDocNo).HasMaxLength(100);
            entity.Property(e => e.Destination).HasMaxLength(200);
            entity.Property(e => e.TermsOfDelivery).HasMaxLength(200);
            entity.Property(e => e.AsnNo).HasMaxLength(100);
            entity.Property(e => e.TransportDetails).HasMaxLength(500);
            entity.Property(e => e.EwbNo).HasMaxLength(100);
            entity.Property(e => e.StoredFilePath).HasMaxLength(500);

            entity.HasOne(e => e.DcLedger)
                .WithMany(l => l.Invoices)
                .HasForeignKey(e => e.DcLedgerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Invoices)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── InspectionReport ─────────────────────────────────
        modelBuilder.Entity<InspectionReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DrawingNo).HasMaxLength(100);
            entity.Property(e => e.Operation).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DcNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IssueNo).HasMaxLength(50);
            entity.Property(e => e.BatchNo).HasMaxLength(50);
            entity.Property(e => e.ParametersJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.VendorResult).HasMaxLength(200);
            entity.Property(e => e.CieResult).HasMaxLength(200);
            entity.Property(e => e.InspectedBy).HasMaxLength(100);
            entity.Property(e => e.ApprovedBy).HasMaxLength(100);
            entity.Property(e => e.StoredFilePath).HasMaxLength(500);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.InspectionReports)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DcLedger)
                .WithMany(l => l.InspectionReports)
                .HasForeignKey(e => e.DcLedgerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.InspectionReports)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.InspectionReports)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Seed Default Admin User ─────────────────────────
        // Pre-computed BCrypt hash of "Admin@123" (static to avoid PendingModelChangesWarning)
        var adminId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminId,
            Username = "admin",
            PasswordHash = "$2a$11$4EZ9QdbSPoWe65XQAA2kLuWSWdXbs1ZbHQ4A3O8iv3Se6ogLgMOBW",
            Role = "Admin",
            Email = "admin@srimuni.com",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
