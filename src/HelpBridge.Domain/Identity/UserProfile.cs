using HelpBridge.Domain.Common;

namespace HelpBridge.Domain.Identity;

public class UserProfile : BaseEntity<string>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "IN";
    public string? IdentityDocType { get; set; }
    public string? IdentityDocMaskedNumber { get; set; }
    public bool IdentityVerified { get; set; } = false;
    public string PreferredCurrency { get; set; } = "INR";

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => $"{FirstName} {(!string.IsNullOrEmpty(LastName) ? LastName[0] + "." : "")}".Trim();

    public UserProfile() { }

    public UserProfile(string userId, string firstName, string lastName, string city, string state)
    {
        Id = userId;
        FirstName = firstName;
        LastName = lastName;
        City = city;
        State = state;
        CreatedUtc = DateTime.UtcNow;
    }
}

