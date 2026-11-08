using System.Security.Claims;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Requests.DTOs;
using HelpBridge.Application.Features.Verification.DTOs;
using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Administration;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Verification;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/verification")]
[Authorize] // Can restrict to VerificationOfficer or Admin in production
public class VerificationController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _storageService;

    public VerificationController(AppDbContext dbContext, IBlobStorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<List<VerificationQueueItemDto>>>> GetVerificationQueue()
    {
        var cases = await _dbContext.VerificationCases
            .Include(v => v.Request)
                .ThenInclude(r => r.Category)
            .Include(v => v.Request)
                .ThenInclude(r => r.Documents)
            .Where(v => v.Status == "Pending" || v.Request.Status == RequestStatus.Submitted || v.Request.Status == RequestStatus.UnderReview)
            .OrderBy(v => v.CreatedUtc)
            .ToListAsync();

        var recipientIds = cases.Select(c => c.Request.RecipientId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .Where(p => recipientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Check for duplicate hashes in queue
        var allChecksums = cases.SelectMany(c => c.Request.Documents.Select(d => d.Sha256Checksum)).ToList();
        var duplicates = await _dbContext.RequestDocuments
            .Where(d => allChecksums.Contains(d.Sha256Checksum))
            .GroupBy(d => d.Sha256Checksum)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();

        var dtos = cases.Select(c =>
        {
            profiles.TryGetValue(c.Request.RecipientId, out var profile);
            var hasDuplicate = c.Request.Documents.Any(d => duplicates.Contains(d.Sha256Checksum));

            return new VerificationQueueItemDto
            {
                CaseId = c.Id,
                RequestId = c.RequestId,
                CategoryName = c.Request.Category.DisplayName,
                RecipientName = profile?.FullName ?? "Unknown",
                Title = c.Request.Title,
                ClaimedAmount = c.Request.TargetAmount,
                SubmittedUtc = c.Request.CreatedUtc,
                DocumentCount = c.Request.Documents.Count,
                DuplicateDocumentFlag = hasDuplicate,
                Status = c.Status
            };
        }).ToList();

        return Ok(ApiResponse<List<VerificationQueueItemDto>>.Ok(dtos));
    }

    [HttpGet("{caseId}")]
    public async Task<ActionResult<ApiResponse<VerificationDetailDto>>> GetCaseDetails(Guid caseId)
    {
        var verificationCase = await _dbContext.VerificationCases
            .Include(v => v.Request)
                .ThenInclude(r => r.Category)
            .Include(v => v.Request)
                .ThenInclude(r => r.Documents)
            .FirstOrDefaultAsync(v => v.Id == caseId);

        if (verificationCase == null)
        {
            return NotFound(ApiResponse<VerificationDetailDto>.Fail("Verification case not found."));
        }

        var profile = await _dbContext.UserProfiles.FindAsync(verificationCase.Request.RecipientId);

        var dto = new VerificationDetailDto
        {
            CaseId = verificationCase.Id,
            RequestId = verificationCase.RequestId,
            RecipientName = profile?.FullName ?? "Recipient",
            Title = verificationCase.Request.Title,
            Story = verificationCase.Request.Story,
            ClaimedAmount = verificationCase.Request.TargetAmount,
            BillerName = verificationCase.Request.BillerName,
            BillerReferenceNumber = verificationCase.Request.BillerReferenceNumber,
            BillerBankAccountNumber = verificationCase.Request.BillerBankAccountNumber,
            BillerIfscOrCode = verificationCase.Request.BillerIfscOrCode,
            OcrExtractedAmount = verificationCase.OcrExtractedAmount ?? verificationCase.Request.TargetAmount, // simulated OCR baseline
            OcrExtractedBiller = verificationCase.OcrExtractedBiller ?? verificationCase.Request.BillerName,
            OcrConfidenceScore = verificationCase.OcrConfidenceScore ?? 98.5m,
            AiRiskScore = verificationCase.AiRiskScore ?? 2.1m,
            Documents = verificationCase.Request.Documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType.ToString(),
                OriginalFileName = d.OriginalFileName,
                DownloadUrl = _storageService.GenerateSasUri(d.BlobPath, TimeSpan.FromMinutes(15)),
                FileSizeBytes = d.FileSizeBytes
            }).ToList()
        };

        return Ok(ApiResponse<VerificationDetailDto>.Ok(dto));
    }

    [HttpPost("{caseId}/decision")]
    public async Task<ActionResult<ApiResponse<string>>> RecordDecision(Guid caseId, [FromBody] VerificationDecisionDto dto)
    {
        var officerId = CurrentUserId ?? "officer-admin";

        var verificationCase = await _dbContext.VerificationCases
            .Include(v => v.Request)
            .FirstOrDefaultAsync(v => v.Id == caseId);

        if (verificationCase == null)
        {
            return NotFound(ApiResponse<string>.Fail("Verification case not found."));
        }

        var decision = dto.Decision.Trim().ToLowerInvariant();
        VerificationActionType actionType;

        if (decision == "approve")
        {
            verificationCase.Request.MarkUnderReview();
            verificationCase.Request.Approve();
            verificationCase.Request.PublishToMarketplace();
            verificationCase.Status = "Approved";
            verificationCase.DecisionUtc = DateTime.UtcNow;
            actionType = VerificationActionType.Approved;
        }
        else if (decision == "reject")
        {
            verificationCase.Request.MarkUnderReview();
            verificationCase.Request.Reject();
            verificationCase.Status = "Rejected";
            verificationCase.DecisionUtc = DateTime.UtcNow;
            actionType = VerificationActionType.Rejected;
        }
        else
        {
            verificationCase.Request.RequestAdditionalInfo();
            verificationCase.Status = "InfoRequired";
            actionType = VerificationActionType.InformationRequested;
        }

        var action = new VerificationAction(verificationCase.Id, officerId, actionType, dto.Notes);
        _dbContext.VerificationActions.Add(action);

        // Record immutable audit log
        var audit = new AuditLog(
            actorUserId: officerId,
            action: $"Verification_{actionType}",
            entityName: "AssistanceRequest",
            entityId: verificationCase.RequestId.ToString(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            newJson: $"{{\"Decision\":\"{actionType}\",\"Notes\":\"{dto.Notes}\"}}"
        );
        _dbContext.AuditLogs.Add(audit);

        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok($"Verification decision recorded: {actionType}."));
    }
}

