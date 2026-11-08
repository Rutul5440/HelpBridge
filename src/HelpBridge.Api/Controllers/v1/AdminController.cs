using HelpBridge.Application.Common;
using HelpBridge.Domain.Administration;
using HelpBridge.Domain.Enums;
using HelpBridge.Infrastructure.Identity;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/admin")]
[Authorize] // Can restrict to SuperAdmin in production
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardMetricsDto>>> GetDashboardMetrics()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var totalRequests = await _dbContext.AssistanceRequests.CountAsync();
        var pendingVerification = await _dbContext.VerificationCases.CountAsync(v => v.Status == "Pending");
        var activeRequests = await _dbContext.AssistanceRequests.CountAsync(r => r.Status == RequestStatus.Active || r.Status == RequestStatus.PartiallyFunded);
        var completedRequests = await _dbContext.AssistanceRequests.CountAsync(r => r.Status == RequestStatus.Completed);
        var readyForPayout = await _dbContext.Payouts.CountAsync(p => p.Status == PayoutStatus.ReadyForPayout);

        var totalContributionsFacilitated = await _dbContext.Donations
            .Where(d => d.Status == DonationStatus.Confirmed)
            .SumAsync(d => (decimal?)d.AssistanceAmount) ?? 0m;

        var totalPlatformTips = await _dbContext.Donations
            .Where(d => d.Status == DonationStatus.Confirmed)
            .SumAsync(d => (decimal?)d.PlatformTipAmount) ?? 0m;

        // Ledger Health Check: Check if all debits == all credits across the whole system
        var totalDebits = await _dbContext.JournalLineItems.SumAsync(l => (decimal?)l.DebitAmount) ?? 0m;
        var totalCredits = await _dbContext.JournalLineItems.SumAsync(l => (decimal?)l.CreditAmount) ?? 0m;
        var isLedgerBalanced = Math.Abs(totalDebits - totalCredits) < 0.0001m;

        var metrics = new AdminDashboardMetricsDto
        {
            TotalRegisteredUsers = totalUsers,
            TotalAssistanceRequests = totalRequests,
            PendingVerificationCases = pendingVerification,
            ActiveMarketplaceRequests = activeRequests,
            CompletedRequests = completedRequests,
            PendingPayoutsCount = readyForPayout,
            TotalContributionsFacilitated = totalContributionsFacilitated,
            TotalPlatformTipsRevenue = totalPlatformTips,
            IsLedgerBalanced = isLedgerBalanced,
            LedgerVariance = Math.Abs(totalDebits - totalCredits)
        };

        return Ok(ApiResponse<AdminDashboardMetricsDto>.Ok(metrics));
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetAuditLogs([FromQuery] int limit = 50)
    {
        var logs = await _dbContext.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .Take(limit)
            .ToListAsync();

        return Ok(ApiResponse<List<AuditLog>>.Ok(logs));
    }
}

public class AdminDashboardMetricsDto
{
    public int TotalRegisteredUsers { get; set; }
    public int TotalAssistanceRequests { get; set; }
    public int PendingVerificationCases { get; set; }
    public int ActiveMarketplaceRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int PendingPayoutsCount { get; set; }
    public decimal TotalContributionsFacilitated { get; set; }
    public decimal TotalPlatformTipsRevenue { get; set; }
    public bool IsLedgerBalanced { get; set; }
    public decimal LedgerVariance { get; set; }
}

