using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payments;

namespace HelpBridge.Domain.Donations;

public class DonationIntent : BaseEntity
{
    public string DonorId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public decimal AssistanceAmount { get; set; }
    public decimal TipAmount { get; set; } = 0.0000m;
    public decimal TotalAmount => AssistanceAmount + TipAmount;
    public string Currency { get; set; } = "INR";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public string Status { get; set; } = "Pending";

    public AssistanceRequest Request { get; set; } = null!;
    public Donation? Donation { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresUtc;

    public DonationIntent() { }

    public DonationIntent(string donorId, Guid requestId, decimal assistanceAmount, decimal tipAmount, string idempotencyKey, int expirationMinutes = 15)
    {
        DonorId = donorId;
        RequestId = requestId;
        AssistanceAmount = assistanceAmount;
        TipAmount = tipAmount;
        IdempotencyKey = idempotencyKey;
        ExpiresUtc = DateTime.UtcNow.AddMinutes(expirationMinutes);
        Status = "Pending";
        CreatedUtc = DateTime.UtcNow;
    }
}

public class Donation : BaseEntity, IAggregateRoot
{
    public Guid IntentId { get; set; }
    public string DonorId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal AssistanceAmount { get; set; }
    public decimal PlatformTipAmount { get; set; } = 0.0000m;
    public string Currency { get; set; } = "INR";
    public bool IsAnonymous { get; set; } = false;
    public DonationStatus Status { get; set; } = DonationStatus.Confirmed;
    public DateTime ConfirmedUtc { get; set; } = DateTime.UtcNow;

    public DonationIntent Intent { get; set; } = null!;
    public AssistanceRequest Request { get; set; } = null!;
    public PaymentTransaction? PaymentTransaction { get; set; }

    public Donation() { }

    public Donation(Guid intentId, string donorId, Guid requestId, decimal assistanceAmount, decimal tipAmount, bool isAnonymous = false)
    {
        IntentId = intentId;
        DonorId = donorId;
        RequestId = requestId;
        AssistanceAmount = assistanceAmount;
        PlatformTipAmount = tipAmount;
        GrossAmount = assistanceAmount + tipAmount;
        IsAnonymous = isAnonymous;
        Status = DonationStatus.Confirmed;
        ConfirmedUtc = DateTime.UtcNow;
    }
}

public class SavedRequest : BaseEntity<long>
{
    public string DonorId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }

    public AssistanceRequest Request { get; set; } = null!;

    public SavedRequest() { }

    public SavedRequest(string donorId, Guid requestId)
    {
        DonorId = donorId;
        RequestId = requestId;
        CreatedUtc = DateTime.UtcNow;
    }
}

