using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Identity.DTOs;
using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Identity;
using HelpBridge.Infrastructure.Identity;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _dbContext;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Passwords do not match."));
        }

        if (!dto.TermsAccepted)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("You must accept the Terms of Service and Privacy Policy."));
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("An account with this email address already exists."));
        }

        var targetRole = dto.Role.Equals("Recipient", StringComparison.OrdinalIgnoreCase) ? "Recipient" : "Donor";

        if (!await _roleManager.RoleExistsAsync(targetRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(targetRole));
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = true // Sandbox/dev auto-confirm; production sends verification token
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(errors));
        }

        await _userManager.AddToRoleAsync(user, targetRole);

        // Create User Profile
        var profile = new UserProfile(user.Id, dto.FirstName, dto.LastName, dto.City, dto.State);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email!,
            FullName = profile.FullName,
            Roles = roles.ToList()
        };

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Registration successful."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password."));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password."));
        }

        var profile = await _dbContext.UserProfiles.FindAsync(user.Id);
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email!,
            FullName = profile?.FullName ?? user.UserName ?? "User",
            Roles = roles.ToList()
        };

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("User is not authenticated."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail("User account not found."));
        }

        var profile = await _dbContext.UserProfiles.FindAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        var response = new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = profile?.FirstName ?? "",
            LastName = profile?.LastName ?? "",
            City = profile?.City ?? "",
            State = profile?.State ?? "",
            PreferredCurrency = profile?.PreferredCurrency ?? "INR",
            IdentityVerified = profile?.IdentityVerified ?? false,
            Roles = roles.ToList()
        };

        return Ok(ApiResponse<UserProfileDto>.Ok(response));
    }
}

