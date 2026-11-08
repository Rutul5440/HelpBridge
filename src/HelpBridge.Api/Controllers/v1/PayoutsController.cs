using System.Security.Claims;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Payouts.DTOs;
using HelpBridge.Domain.Accounting;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payouts;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/payouts")]
[Authorize] // Can restrict to FinanceManager in production
public class PayoutsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PayoutsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<List<PayoutQueueItemDto>>>> GetPayoutQueue()
    {
        var payouts = await _dbContext.Payouts
            .Include(p => p.Request)
            .Where(p => p.Status == PayoutStatus.ReadyForPayout || p.Status == PayoutStatus.PayoutProcessing)
            .OrderBy(p => p.CreatedUtc)
            .ToListAsync();

        var recipientIds = payouts.Select(p => p.Request.RecipientId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .Where(pr => recipientIds.Contains(pr.Id))
            .ToDictionaryAsync(pr => pr.Id);

        var dtos = payouts.Select(p =>
        {
            profiles.TryGetValue(p.Request.RecipientId, out var profile);
            return new PayoutQueueItemDto
            {
                PayoutId = p.Id,
                RequestId = p.RequestId,
                RequestTitle = p.Request.Title,
                RecipientName = profile?.FullName ?? "Recipient",
                BillerName = p.Request.BillerName,
                BillerBankAccountNumber = p.Request.BillerBankAccountNumber,
                BillerIfscOrCode = p.Request.BillerIfscOrCode,
                DisbursedAmount = p.DisbursedAmount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                CreatedUtc = p.CreatedUtc
            };
        }).ToList();

        return Ok(ApiResponse<List<PayoutQueueItemDto>>.Ok(dtos));
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmPayout(Guid id, [FromBody] ConfirmPayoutDto dto)
    {
        var financeOfficerId = CurrentUserId ?? "finance-manager-admin";

        if (string.IsNullOrWhiteSpace(dto.BankUtrNumber))
        {
            return BadRequest(ApiResponse<string>.Fail("Official bank UTR number is required for payout confirmation."));
        }

        var payout = await _dbContext.Payouts
            .Include(p => p.Request)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payout == null)
        {
            return NotFound(ApiResponse<string>.Fail("Payout record not found."));
        }

        // Maker-Checker validation
        if (payout.InitiatedByUserId == financeOfficerId && payout.InitiatedByUserId != "system-auto-queue")
        {
            return BadRequest(ApiResponse<string>.Fail("Maker-Checker violation: Payout approver cannot be the same as payout initiator."));
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            payout.ProcessPayout(financeOfficerId);
            payout.MarkSuccess();

            var confirmation = new PayoutConfirmation(
                payoutId: payout.Id,
                bankUtrNumber: dto.BankUtrNumber,
                paymentMode: dto.PaymentMode,
                receiptBlobPath: dto.ReceiptBlobPath
            );

            _dbContext.PayoutConfirmations.Add(confirmation);

            // Transition AssistanceRequest to Completed
            payout.Request.MarkCompleted();

            // Double-Entry Ledger Entry for Payout
            var journalEntry = new JournalEntry("Payout", payout.Id, $"Direct disbursement to {payout.Request.BillerName} (UTR: {dto.BankUtrNumber})");
            
            // Debit: 2010-Recipient-Escrow-Liability (Discharging the held liability)
            journalEntry.AddDebit(ledgerAccountId: 2010, amount: payout.DisbursedAmount);

            // Credit: 1000-Bank-Operating (Funds paid out from bank account)
            journalEntry.AddCredit(ledgerAccountId: 1000, amount: payout.DisbursedAmount);

            journalEntry.ValidateBalance();
            _dbContext.JournalEntries.Add(journalEntry);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<string>.Ok($"Payout confirmed successfully with UTR {dto.BankUtrNumber}. Request marked as Completed."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
