using HelpBridge.Domain.Common;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payouts;
using HelpBridge.Domain.Verification;

namespace HelpBridge.Domain.Assistance;

public class AssistanceRequest : BaseEntity, IAggregateRoot
{
    public string RecipientId { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal FundedAmount { get; protected set; } = 0.0000m;
    public string Currency { get; set; } = "INR";
    public RequestStatus Status { get; protected set; } = RequestStatus.Draft;
    public DateTime DueDate { get; set; }

    // Direct Biller / Institution Details
    public string BillerName { get; set; } = string.Empty;
    public string BillerReferenceNumber { get; set; } = string.Empty;
    public string BillerBankAccountNumber { get; set; } = string.Empty;
    public string BillerIfscOrCode { get; set; } = string.Empty;

    public DateTime? VerifiedUtc { get; protected set; }
    public DateTime? CompletedUtc { get; protected set; }
    public byte[]? RowVersion { get; set; }

    // Navigations
    public AssistanceCategory Category { get; set; } = null!;
    public ICollection<RequestDocument> Documents { get; set; } = [];
    public VerificationCase? VerificationCase { get; set; }
    public ICollection<Donation> Donations { get; set; } = [];
    public ICollection<PayoutRecord> Payouts { get; set; } = [];

    public decimal RemainingAmount => Math.Max(0m, TargetAmount - FundedAmount);
    public decimal ProgressPercentage => TargetAmount > 0 ? Math.Min(100m, Math.Round((FundedAmount / TargetAmount) * 100m, 2)) : 0m;
    public bool IsFundable => Status == RequestStatus.Active || Status == RequestStatus.PartiallyFunded;

    public AssistanceRequest() { }

    public AssistanceRequest(string recipientId, int categoryId, string title, string story, decimal targetAmount, DateTime dueDate, string billerName, string billerRef, string billerAccount, string billerIfsc)
    {
        RecipientId = recipientId;
        CategoryId = categoryId;
        Title = title;
        Story = story;
        TargetAmount = targetAmount;
        DueDate = dueDate;
        BillerName = billerName;
        BillerReferenceNumber = billerRef;
        BillerBankAccountNumber = billerAccount;
        BillerIfscOrCode = billerIfsc;
        Status = RequestStatus.Draft;
        CreatedUtc = DateTime.UtcNow;
    }

    public void Submit()
    {
        if (Status != RequestStatus.Draft && Status != RequestStatus.InfoRequired)
            throw new InvalidOperationException($"Cannot submit request in status '{Status}'. Must be Draft or InfoRequired.");

        if (Documents.Count == 0)
            throw new InvalidOperationException("At least one supporting institutional document must be uploaded before submitting.");

        Status = RequestStatus.Submitted;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkUnderReview()
    {
        if (Status != RequestStatus.Submitted)
            throw new InvalidOperationException($"Cannot transition request to UnderReview from status '{Status}'.");

        Status = RequestStatus.UnderReview;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void RequestAdditionalInfo()
    {
        if (Status != RequestStatus.UnderReview)
            throw new InvalidOperationException($"Cannot request additional info unless under review.");

        Status = RequestStatus.InfoRequired;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != RequestStatus.UnderReview)
            throw new InvalidOperationException($"Cannot approve request from status '{Status}'. Must be UnderReview.");

        Status = RequestStatus.Verified;
        VerifiedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void PublishToMarketplace()
    {
        if (Status != RequestStatus.Verified)
            throw new InvalidOperationException($"Cannot publish request to marketplace unless Verified.");

        Status = RequestStatus.Active;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != RequestStatus.UnderReview && Status != RequestStatus.Submitted)
            throw new InvalidOperationException($"Cannot reject request in status '{Status}'.");

        Status = RequestStatus.Rejected;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void AddContribution(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Contribution amount must be greater than zero.");

        if (!IsFundable)
            throw new InvalidOperationException($"Cannot accept contributions for request in status '{Status}'.");

        if (FundedAmount + amount > TargetAmount)
            throw new InvalidOperationException($"Contribution of ₹{amount} exceeds remaining target of ₹{RemainingAmount}.");

        FundedAmount += amount;
        UpdatedUtc = DateTime.UtcNow;

        if (FundedAmount >= TargetAmount)
        {
            Status = RequestStatus.FullyFunded;
        }
        else
        {
            Status = RequestStatus.PartiallyFunded;
        }
    }

    public void QueueForPayout()
    {
        if (Status != RequestStatus.FullyFunded)
            throw new InvalidOperationException($"Cannot queue for payout unless FullyFunded. Current status: '{Status}'.");

        Status = RequestStatus.ReadyForPayout;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        if (Status != RequestStatus.PayoutProcessing && Status != RequestStatus.ReadyForPayout)
            throw new InvalidOperationException($"Cannot complete request unless payout was processing. Current status: '{Status}'.");

        Status = RequestStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Suspend(string reason)
    {
        Status = RequestStatus.Suspended;
        UpdatedUtc = DateTime.UtcNow;
    }
}

