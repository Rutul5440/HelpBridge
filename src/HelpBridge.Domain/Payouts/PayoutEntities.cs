using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Payouts;

public class PayoutRecord : BaseEntity
{
    public Guid RequestId { get; set; }
    public decimal DisbursedAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public PayoutStatus Status { get; set; } = PayoutStatus.ReadyForPayout;
    public string InitiatedByUserId { get; set; } = string.Empty;
    public string? ApprovedByUserId { get; set; } // Maker-Checker
    public DateTime? CompletedUtc { get; set; }

    public AssistanceRequest Request { get; set; } = null!;
    public PayoutConfirmation? Confirmation { get; set; }

    public PayoutRecord() { }

    public PayoutRecord(Guid requestId, decimal disbursedAmount, string initiatedByUserId)
    {
        RequestId = requestId;
        DisbursedAmount = disbursedAmount;
        InitiatedByUserId = initiatedByUserId;
        Status = PayoutStatus.ReadyForPayout;
        CreatedUtc = DateTime.UtcNow;
    }

    public void ProcessPayout(string approvedByUserId)
    {
        if (InitiatedByUserId == approvedByUserId)
            throw new InvalidOperationException("Maker-Checker violation: Payout approver cannot be the same as payout initiator.");

        ApprovedByUserId = approvedByUserId;
        Status = PayoutStatus.PayoutProcessing;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkSuccess()
    {
        Status = PayoutStatus.PayoutSuccess;
        CompletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = PayoutStatus.PayoutFailed;
        UpdatedUtc = DateTime.UtcNow;
    }
}

public class PayoutConfirmation : BaseEntity
{
    public Guid PayoutId { get; set; }
    public string BankUtrNumber { get; set; } = string.Empty;
    public string? ReceiptBlobPath { get; set; }
    public string PaymentMode { get; set; } = "NEFT"; // NEFT, RTGS, IMPS, DirectTransfer
    public DateTime ConfirmedUtc { get; set; } = DateTime.UtcNow;

    public PayoutRecord Payout { get; set; } = null!;

    public PayoutConfirmation() { }

    public PayoutConfirmation(Guid payoutId, string bankUtrNumber, string paymentMode, string? receiptBlobPath = null)
    {
        PayoutId = payoutId;
        BankUtrNumber = bankUtrNumber;
        PaymentMode = paymentMode;
        ReceiptBlobPath = receiptBlobPath;
        ConfirmedUtc = DateTime.UtcNow;
        CreatedUtc = DateTime.UtcNow;
    }
}

