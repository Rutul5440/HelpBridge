namespace HelpBridge.Application.Features.Identity.DTOs;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Donor"; // Recipient or Donor
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool TermsAccepted { get; set; } = false;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public int ExpiresInSeconds { get; set; } = 900;
}

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PreferredCurrency { get; set; } = "INR";
    public bool IdentityVerified { get; set; }
    public List<string> Roles { get; set; } = [];
}

