using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Assistance;

public class AssistanceCategory : BaseEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MaxAllowedAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public AssistanceCategoryType Type { get; set; }

    public ICollection<AssistanceRequest> Requests { get; set; } = [];
}

