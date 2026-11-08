using HelpBridge.Domain.Accounting;
using HelpBridge.Domain.Administration;
using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Identity;
using HelpBridge.Domain.Payments;
using HelpBridge.Domain.Payouts;
using HelpBridge.Domain.Verification;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<AssistanceCategory> AssistanceCategories { get; }
    DbSet<AssistanceRequest> AssistanceRequests { get; }
    DbSet<RequestDocument> RequestDocuments { get; }
    DbSet<VerificationCase> VerificationCases { get; }
    DbSet<VerificationAction> VerificationActions { get; }
    DbSet<DonationIntent> DonationIntents { get; }
    DbSet<Donation> Donations { get; }
    DbSet<SavedRequest> SavedRequests { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<PaymentWebhook> PaymentWebhooks { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<PayoutRecord> Payouts { get; }
    DbSet<PayoutConfirmation> PayoutConfirmations { get; }
    DbSet<LedgerAccount> LedgerAccounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalLineItem> JournalLineItems { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SupportTicket> SupportTickets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
}

public interface ITokenService
{
    string GenerateAccessToken(string userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default);
    string GenerateSasUri(string blobPath, TimeSpan validDuration);
    Task<bool> DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receiptId, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string rawPayload, string signature, string secret);
}

public record PaymentOrderResult(string OrderId, string ProviderName, decimal Amount, string Currency);

public interface IDocumentAnalysisService
{
    Task<DocumentAnalysisResult> AnalyzeDocumentAsync(string originalFileName, string contentType, Stream documentStream, decimal claimedAmount, string claimedBiller, CancellationToken cancellationToken = default);
}

public record DocumentAnalysisResult(
    decimal? ExtractedAmount,
    string? ExtractedBiller,
    string? ExtractedReferenceNumber,
    DateTime? ExtractedDueDate,
    decimal ConfidenceScore,
    decimal AiRiskScore,
    List<string> RiskFlags
);

