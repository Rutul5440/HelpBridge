using HelpBridge.Domain.Enums;

namespace HelpBridge.Application.Features.Requests.DTOs;

public class CreateRequestDto
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string BillerName { get; set; } = string.Empty;
    public string BillerReferenceNumber { get; set; } = string.Empty;
    public string BillerBankAccountNumber { get; set; } = string.Empty;
    public string BillerIfscOrCode { get; set; } = string.Empty;
}

public class AssistanceRequestDto
{
    public Guid Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientDisplayName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal FundedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string BillerName { get; set; } = string.Empty;
    public string BillerReferenceNumber { get; set; } = string.Empty;
    public int SupporterCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<DocumentDto> Documents { get; set; } = [];
}

public class RequestSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string RecipientDisplayName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal FundedAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int SupporterCount { get; set; }
    public int DaysRemaining { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsVerified { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

