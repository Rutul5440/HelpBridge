using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Accounting;
using HelpBridge.Domain.Administration;
using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Identity;
using HelpBridge.Domain.Notifications;
using HelpBridge.Domain.Payments;
using HelpBridge.Domain.Payouts;
using HelpBridge.Domain.Verification;
using HelpBridge.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AssistanceCategory> AssistanceCategories => Set<AssistanceCategory>();
    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();
    public DbSet<RequestDocument> RequestDocuments => Set<RequestDocument>();
    public DbSet<VerificationCase> VerificationCases => Set<VerificationCase>();
    public DbSet<VerificationAction> VerificationActions => Set<VerificationAction>();
    public DbSet<DonationIntent> DonationIntents => Set<DonationIntent>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<SavedRequest> SavedRequests => Set<SavedRequest>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhook> PaymentWebhooks => Set<PaymentWebhook>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<PayoutRecord> Payouts => Set<PayoutRecord>();
    public DbSet<PayoutConfirmation> PayoutConfirmations => Set<PayoutConfirmation>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLineItem> JournalLineItems => Set<JournalLineItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure UserProfile
        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.City).HasMaxLength(100).IsRequired();
            entity.Property(p => p.State).HasMaxLength(100).IsRequired();
            entity.Property(p => p.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(p => p.PreferredCurrency).HasMaxLength(3).IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(p => p.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AssistanceCategory
        builder.Entity<AssistanceCategory>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Code).HasMaxLength(50).IsRequired();
            entity.Property(c => c.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.MaxAllowedAmount).HasPrecision(18, 4);
        });

        // Configure AssistanceRequest
        builder.Entity<AssistanceRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).HasMaxLength(150).IsRequired();
            entity.Property(r => r.TargetAmount).HasPrecision(18, 4);
            entity.Property(r => r.FundedAmount).HasPrecision(18, 4);
            entity.Property(r => r.Currency).HasMaxLength(3).IsRequired();
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.RowVersion).IsRowVersion();

            entity.HasOne(r => r.Category)
                .WithMany(c => c.Requests)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.RecipientId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CategoryId);
        });

        // Configure RequestDocument
        builder.Entity<RequestDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(50);
            entity.Property(d => d.BlobPath).HasMaxLength(1000).IsRequired();
            entity.Property(d => d.Sha256Checksum).HasMaxLength(64).IsRequired();
            entity.HasIndex(d => d.Sha256Checksum); // Rapid duplicate detection

            entity.HasOne(d => d.Request)
                .WithMany(r => r.Documents)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure VerificationCase & Actions
        builder.Entity<VerificationCase>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.RequestId).IsUnique();
            entity.Property(v => v.OcrExtractedAmount).HasPrecision(18, 4);
            entity.Property(v => v.OcrConfidenceScore).HasPrecision(5, 2);
            entity.Property(v => v.AiRiskScore).HasPrecision(5, 2);

            entity.HasOne(v => v.Request)
                .WithOne(r => r.VerificationCase)
                .HasForeignKey<VerificationCase>(v => v.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VerificationAction>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ActionType).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(a => a.VerificationCase)
                .WithMany(v => v.Actions)
                .HasForeignKey(a => a.VerificationCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DonationIntent
        builder.Entity<DonationIntent>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.IdempotencyKey).IsUnique();
            entity.Property(i => i.AssistanceAmount).HasPrecision(18, 4);
            entity.Property(i => i.TipAmount).HasPrecision(18, 4);
            entity.Property(i => i.Currency).HasMaxLength(3);

            entity.HasOne(i => i.Request)
                .WithMany()
                .HasForeignKey(i => i.RequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Donation
        builder.Entity<Donation>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.GrossAmount).HasPrecision(18, 4);
            entity.Property(d => d.AssistanceAmount).HasPrecision(18, 4);
            entity.Property(d => d.PlatformTipAmount).HasPrecision(18, 4);
            entity.Property(d => d.Currency).HasMaxLength(3);
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(d => d.Intent)
                .WithOne(i => i.Donation)
                .HasForeignKey<Donation>(d => d.IntentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Request)
                .WithMany(r => r.Donations)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SavedRequest
        builder.Entity<SavedRequest>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.DonorId, s.RequestId }).IsUnique();

            entity.HasOne(s => s.Request)
                .WithMany()
                .HasForeignKey(s => s.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PaymentTransaction & Webhooks
        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.GatewayPaymentId).IsUnique().HasFilter("[GatewayPaymentId] IS NOT NULL");
            entity.Property(p => p.PaymentStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(p => p.GatewayFee).HasPrecision(18, 4);
            entity.Property(p => p.GatewayTax).HasPrecision(18, 4);

            entity.HasOne(p => p.Donation)
                .WithOne(d => d.PaymentTransaction)
                .HasForeignKey<PaymentTransaction>(p => p.DonationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentWebhook>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.EventId).IsUnique();
        });

        builder.Entity<Refund>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Amount).HasPrecision(18, 4);

            entity.HasOne(r => r.PaymentTransaction)
                .WithMany(p => p.Refunds)
                .HasForeignKey(r => r.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Payouts
        builder.Entity<PayoutRecord>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisbursedAmount).HasPrecision(18, 4);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(p => p.Currency).HasMaxLength(3);

            entity.HasOne(p => p.Request)
                .WithMany(r => r.Payouts)
                .HasForeignKey(p => p.RequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PayoutConfirmation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.BankUtrNumber).IsUnique();

            entity.HasOne(c => c.Payout)
                .WithOne(p => p.Confirmation)
                .HasForeignKey<PayoutConfirmation>(c => c.PayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Accounting / Double-Entry Ledger
        builder.Entity<LedgerAccount>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.AccountCode).IsUnique();
            entity.Property(a => a.AccountCode).HasMaxLength(50).IsRequired();
            entity.Property(a => a.AccountName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.AccountType).HasConversion<string>().HasMaxLength(50);
            entity.Property(a => a.Balance).HasPrecision(18, 4);
        });

        builder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.ReferenceType).HasMaxLength(50);
            entity.Property(j => j.Description).HasMaxLength(255);
        });

        builder.Entity<JournalLineItem>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.DebitAmount).HasPrecision(18, 4);
            entity.Property(l => l.CreditAmount).HasPrecision(18, 4);

            entity.HasOne(l => l.JournalEntry)
                .WithMany(j => j.LineItems)
                .HasForeignKey(l => l.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.LedgerAccount)
                .WithMany(a => a.LineItems)
                .HasForeignKey(l => l.LedgerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Administration
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            entity.HasIndex(a => a.TimestampUtc);
        });

        builder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Subject).HasMaxLength(200).IsRequired();
        });

        // Configure Notifications
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).HasMaxLength(150).IsRequired();
            entity.Property(n => n.Type).HasMaxLength(50).IsRequired();
            entity.HasIndex(n => new { n.UserId, n.IsRead });
            entity.HasIndex(n => n.CreatedUtc);
        });

        // Seed Core Categories
        builder.Entity<AssistanceCategory>().HasData(
            new AssistanceCategory { Id = 1, Code = "EDUCATION", DisplayName = "Education & Tuition", Description = "College tuition, exam fees, textbooks, vocational certification.", MaxAllowedAmount = 250000m, IsActive = true, Type = AssistanceCategoryType.Education },
            new AssistanceCategory { Id = 2, Code = "MEDICAL", DisplayName = "Medical & Healthcare", Description = "Hospital treatment invoices, diagnostic tests, surgeries, prescription supplies.", MaxAllowedAmount = 500000m, IsActive = true, Type = AssistanceCategoryType.Medical },
            new AssistanceCategory { Id = 3, Code = "BILLS", DisplayName = "Essential Utility Bills", Description = "Electricity, municipal water, cooking gas arrears.", MaxAllowedAmount = 50000m, IsActive = true, Type = AssistanceCategoryType.EssentialBills },
            new AssistanceCategory { Id = 4, Code = "HOUSING", DisplayName = "Housing & Rent Relief", Description = "Immediate residential rent arrears to avert eviction.", MaxAllowedAmount = 100000m, IsActive = true, Type = AssistanceCategoryType.Housing }
        );

        // Seed Core Ledger Accounts
        builder.Entity<LedgerAccount>().HasData(
            new LedgerAccount { Id = 1010, AccountCode = "1010-GATEWAY-CLEARING", AccountName = "Gateway Clearing Account", AccountType = LedgerAccountType.Asset, Balance = 0m },
            new LedgerAccount { Id = 1000, AccountCode = "1000-BANK-OPERATING", AccountName = "Primary Operating Bank Account", AccountType = LedgerAccountType.Asset, Balance = 0m },
            new LedgerAccount { Id = 2010, AccountCode = "2010-ESCROW-LIABILITY", AccountName = "Recipient Escrow Assistance Liability", AccountType = LedgerAccountType.Liability, Balance = 0m },
            new LedgerAccount { Id = 4010, AccountCode = "4010-VOLUNTARY-TIPS", AccountName = "Platform Voluntary Tips Revenue", AccountType = LedgerAccountType.Revenue, Balance = 0m },
            new LedgerAccount { Id = 5010, AccountCode = "5010-GATEWAY-EXPENSE", AccountName = "Payment Gateway Processing Expense", AccountType = LedgerAccountType.Expense, Balance = 0m }
        );
    }
}

