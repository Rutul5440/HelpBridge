using System.Security.Claims;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Requests.DTOs;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/discovery")]
public class DiscoveryController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DiscoveryController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<RequestSummaryDto>>>> GetDiscoveryFeed(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = _dbContext.AssistanceRequests
            .Include(r => r.Category)
            .Include(r => r.Donations)
            .Where(r => r.Status == RequestStatus.Active || r.Status == RequestStatus.PartiallyFunded);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(r => r.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(term) || r.BillerName.ToLower().Contains(term));
        }

        var items = await query
            .OrderByDescending(r => r.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var recipientIds = items.Select(i => i.RecipientId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .Where(p => recipientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var dtos = items.Select(r =>
        {
            profiles.TryGetValue(r.RecipientId, out var profile);
            var daysRemaining = Math.Max(0, (r.DueDate - DateTime.UtcNow.Date).Days);

            return new RequestSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                CategoryName = r.Category.DisplayName,
                RecipientDisplayName = profile?.DisplayName ?? "Verified Recipient",
                Location = $"{profile?.City}, {profile?.State}".Trim(',', ' '),
                TargetAmount = r.TargetAmount,
                FundedAmount = r.FundedAmount,
                ProgressPercentage = r.ProgressPercentage,
                SupporterCount = r.Donations?.Count(d => d.Status == DonationStatus.Confirmed) ?? 0,
                DaysRemaining = daysRemaining,
                Currency = r.Currency,
                IsVerified = true
            };
        }).ToList();

        return Ok(ApiResponse<List<RequestSummaryDto>>.Ok(dtos));
    }

    [HttpGet("swipe")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<RequestSummaryDto>>>> GetSwipeDeck([FromQuery] int limit = 10)
    {
        // Donors discover verified causes via swipe cards
        var items = await _dbContext.AssistanceRequests
            .Include(r => r.Category)
            .Include(r => r.Donations)
            .Where(r => r.Status == RequestStatus.Active || r.Status == RequestStatus.PartiallyFunded)
            .OrderBy(r => Guid.NewGuid()) // Random deck distribution
            .Take(limit)
            .ToListAsync();

        var recipientIds = items.Select(i => i.RecipientId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .Where(p => recipientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var dtos = items.Select(r =>
        {
            profiles.TryGetValue(r.RecipientId, out var profile);
            var daysRemaining = Math.Max(0, (r.DueDate - DateTime.UtcNow.Date).Days);

            return new RequestSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                CategoryName = r.Category.DisplayName,
                RecipientDisplayName = profile?.DisplayName ?? "Verified Recipient",
                Location = $"{profile?.City}, {profile?.State}".Trim(',', ' '),
                TargetAmount = r.TargetAmount,
                FundedAmount = r.FundedAmount,
                ProgressPercentage = r.ProgressPercentage,
                SupporterCount = r.Donations?.Count(d => d.Status == DonationStatus.Confirmed) ?? 0,
                DaysRemaining = daysRemaining,
                Currency = r.Currency,
                IsVerified = true
            };
        }).ToList();

        return Ok(ApiResponse<List<RequestSummaryDto>>.Ok(dtos));
    }

    [HttpPost("{id}/save")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> SaveRequest(Guid id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var exists = await _dbContext.SavedRequests.AnyAsync(s => s.DonorId == userId && s.RequestId == id);
        if (exists)
        {
            return Ok(ApiResponse<string>.Ok("Already saved in your bookmarks."));
        }

        var saved = new SavedRequest(userId, id);
        _dbContext.SavedRequests.Add(saved);
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Request saved to your bookmarks."));
    }
}

