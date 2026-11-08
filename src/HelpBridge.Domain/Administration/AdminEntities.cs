using HelpBridge.Domain.Common;

namespace HelpBridge.Domain.Administration;

public class AuditLog : BaseEntity<long>
{
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public AuditLog() { }

    public AuditLog(string? actorUserId, string action, string entityName, string entityId, string ipAddress, string? oldJson = null, string? newJson = null, string? userAgent = null)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        IpAddress = ipAddress;
        OldValuesJson = oldJson;
        NewValuesJson = newJson;
        UserAgent = userAgent;
        TimestampUtc = DateTime.UtcNow;
        CreatedUtc = DateTime.UtcNow;
    }
}

public class SupportTicket : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
    public string? AssignedAgentId { get; set; }

    public SupportTicket() { }

    public SupportTicket(string userId, string subject, string message, string priority = "Normal")
    {
        UserId = userId;
        Subject = subject;
        Message = message;
        Priority = priority;
        Status = "Open";
        CreatedUtc = DateTime.UtcNow;
    }
}

