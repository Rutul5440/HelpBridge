using HelpBridge.Application.Features.Requests.DTOs;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Application.Features.Verification.DTOs;

public class VerificationQueueItemDto
{
    public Guid CaseId { get; set; }
    public Guid RequestId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal ClaimedAmount { get; set; }
    public DateTime SubmittedUtc { get; set; }
    public int DocumentCount { get; set; }
    public bool DuplicateDocumentFlag { get; set; }
    public string Status { get; set; } = "Pending";
}

public class VerificationDetailDto
{
    public Guid CaseId { get; set; }
    public Guid RequestId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public decimal ClaimedAmount { get; set; }
    public string BillerName { get; set; } = string.Empty;
    public string BillerReferenceNumber { get; set; } = string.Empty;
    public string BillerBankAccountNumber { get; set; } = string.Empty;
    public string BillerIfscOrCode { get; set; } = string.Empty;
    public List<DocumentDto> Documents { get; set; } = [];
    public decimal? OcrExtractedAmount { get; set; }
    public string? OcrExtractedBiller { get; set; }
    public decimal? OcrConfidenceScore { get; set; }
    public decimal? AiRiskScore { get; set; }
}

public class VerificationDecisionDto
{
    public Guid CaseId { get; set; }
    public string Decision { get; set; } = string.Empty; // Approve, Reject, RequestInfo
    public string Notes { get; set; } = string.Empty;
}

