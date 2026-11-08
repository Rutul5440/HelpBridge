using HelpBridge.Domain.Common;

namespace HelpBridge.Domain.Notifications;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "GENERAL"; // REQUEST_VERIFIED, CONTRIBUTION_RECEIVED, GOAL_REACHED, PAYOUT_CONFIRMED
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadUtc { get; set; }

    public Notification() { }

    public Notification(string userId, string title, string message, string type, Guid? referenceId = null)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        ReferenceId = referenceId;
        IsRead = false;
        CreatedUtc = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }
}

