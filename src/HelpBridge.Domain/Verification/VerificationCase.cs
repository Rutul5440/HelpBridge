using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Verification;

public class VerificationCase : BaseEntity
{
    public Guid RequestId { get; set; }
    public string? AssignedOfficerId { get; set; }
    public string Status { get; set; } = "Pending";
    
    // OCR & AI extraction fields
    public decimal? OcrExtractedAmount { get; set; }
    public string? OcrExtractedBiller { get; set; }
    public decimal? OcrConfidenceScore { get; set; }
    public decimal? AiRiskScore { get; set; }
    public string? AiRiskFlags { get; set; } // JSON serialized array
    
    public DateTime? DecisionUtc { get; set; }

    public AssistanceRequest Request { get; set; } = null!;
    public ICollection<VerificationAction> Actions { get; set; } = [];

    public VerificationCase() { }

    public VerificationCase(Guid requestId)
    {
        RequestId = requestId;
        Status = "Pending";
        CreatedUtc = DateTime.UtcNow;
    }
}

public class VerificationAction : BaseEntity<long>
{
    public Guid VerificationCaseId { get; set; }
    public string OfficerId { get; set; } = string.Empty;
    public VerificationActionType ActionType { get; set; }
    public string Notes { get; set; } = string.Empty;

    public VerificationCase VerificationCase { get; set; } = null!;

    public VerificationAction() { }

    public VerificationAction(Guid caseId, string officerId, VerificationActionType actionType, string notes)
    {
        VerificationCaseId = caseId;
        OfficerId = officerId;
        ActionType = actionType;
        Notes = notes;
        CreatedUtc = DateTime.UtcNow;
    }
}

