using HelpBridge.Domain.Common;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Payments;

public class PaymentTransaction : BaseEntity
{
    public Guid DonationId { get; set; }
    public string ProviderName { get; set; } = string.Empty; // Razorpay, Cashfree, Simulated
    public string GatewayOrderId { get; set; } = string.Empty;
    public string? GatewayPaymentId { get; set; }
    public PaymentTransactionStatus PaymentStatus { get; set; } = PaymentTransactionStatus.Created;
    public decimal GatewayFee { get; set; } = 0.0000m;
    public decimal GatewayTax { get; set; } = 0.0000m;
    public DateTime? PaidUtc { get; set; }
    public DateTime? ReconciledUtc { get; set; }

    public Donation Donation { get; set; } = null!;
    public ICollection<Refund> Refunds { get; set; } = [];

    public PaymentTransaction() { }

    public PaymentTransaction(Guid donationId, string providerName, string gatewayOrderId)
    {
        DonationId = donationId;
        ProviderName = providerName;
        GatewayOrderId = gatewayOrderId;
        PaymentStatus = PaymentTransactionStatus.Created;
        CreatedUtc = DateTime.UtcNow;
    }

    public void MarkSuccess(string gatewayPaymentId, decimal fee = 0, decimal tax = 0)
    {
        GatewayPaymentId = gatewayPaymentId;
        GatewayFee = fee;
        GatewayTax = tax;
        PaymentStatus = PaymentTransactionStatus.Success;
        PaidUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        PaymentStatus = PaymentTransactionStatus.Failed;
        UpdatedUtc = DateTime.UtcNow;
    }
}

public class PaymentWebhook : BaseEntity<long>
{
    public string ProviderName { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string ProcessedStatus { get; set; } = "Pending"; // Processed, Duplicate, InvalidSignature, Failed
    public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;

    public PaymentWebhook() { }

    public PaymentWebhook(string providerName, string eventId, string eventType, string rawPayload, string signature)
    {
        ProviderName = providerName;
        EventId = eventId;
        EventType = eventType;
        RawPayload = rawPayload;
        Signature = signature;
        ReceivedUtc = DateTime.UtcNow;
        CreatedUtc = DateTime.UtcNow;
    }
}

public class Refund : BaseEntity
{
    public Guid PaymentTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string InitiatedByUserId { get; set; } = string.Empty;
    public string? GatewayRefundId { get; set; }

    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    public Refund() { }

    public Refund(Guid paymentTransactionId, decimal amount, string reason, string initiatedByUserId)
    {
        PaymentTransactionId = paymentTransactionId;
        Amount = amount;
        Reason = reason;
        InitiatedByUserId = initiatedByUserId;
        Status = "Pending";
        CreatedUtc = DateTime.UtcNow;
    }
}

