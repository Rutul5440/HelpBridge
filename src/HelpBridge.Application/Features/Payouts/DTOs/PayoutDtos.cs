namespace HelpBridge.Application.Features.Payouts.DTOs;

public class PayoutQueueItemDto
{
    public Guid PayoutId { get; set; }
    public Guid RequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string BillerName { get; set; } = string.Empty;
    public string BillerBankAccountNumber { get; set; } = string.Empty;
    public string BillerIfscOrCode { get; set; } = string.Empty;
    public decimal DisbursedAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class ConfirmPayoutDto
{
    public Guid PayoutId { get; set; }
    public string BankUtrNumber { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = "NEFT"; // NEFT, RTGS, IMPS
    public string? ReceiptBlobPath { get; set; }
}

