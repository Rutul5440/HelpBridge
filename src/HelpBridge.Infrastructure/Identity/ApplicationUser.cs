using HelpBridge.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HelpBridge.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public UserProfile? UserProfile { get; set; }
}

