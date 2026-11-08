namespace HelpBridge.Application.Features.Donations.DTOs;

public class CreateDonationIntentDto
{
    public Guid RequestId { get; set; }
    public decimal AssistanceAmount { get; set; }
    public decimal PlatformTipAmount { get; set; } = 0.0000m;
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsAnonymous { get; set; } = false;
}

public class DonationIntentResponseDto
{
    public Guid IntentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = "Simulated";
    public decimal AssistanceAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime ExpiresUtc { get; set; }
}

public class DonationReceiptDto
{
    public Guid DonationId { get; set; }
    public Guid RequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime PaidUtc { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed";
}

public class SimulatedPaymentCaptureDto
{
    public Guid IntentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
}

