using System.Security.Claims;
using HelpBridge.Api.Hubs;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Donations.DTOs;
using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Accounting;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payments;
using HelpBridge.Domain.Payouts;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<FundingHub> _fundingHub;

    public PaymentsController(
        AppDbContext dbContext,
        IPaymentProvider paymentProvider,
        IConfiguration configuration,
        IHubContext<FundingHub> fundingHub)
    {
        _dbContext = dbContext;
        _paymentProvider = paymentProvider;
        _configuration = configuration;
        _fundingHub = fundingHub;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    [HttpPost("simulated/capture")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DonationReceiptDto>>> CaptureSimulatedPayment([FromBody] SimulatedPaymentCaptureDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var intent = await _dbContext.DonationIntents
            .Include(i => i.Donation)
            .Include(i => i.Request)
            .FirstOrDefaultAsync(i => i.Id == dto.IntentId);

        if (intent == null)
        {
            return NotFound(ApiResponse<DonationReceiptDto>.Fail("Donation intent not found."));
        }

        if (intent.DonorId != userId)
        {
            return Forbid();
        }

        if (intent.IsExpired)
        {
            return BadRequest(ApiResponse<DonationReceiptDto>.Fail("Donation intent has expired. Please initiate a new contribution."));
        }

        if (intent.Donation != null && intent.Donation.Status == DonationStatus.Confirmed)
        {
            return BadRequest(ApiResponse<DonationReceiptDto>.Fail("This contribution has already been completed."));
        }

        // Use database transaction for atomic ledger and funding consistency
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var mockPaymentId = $"pay_sim_{Guid.NewGuid():N}";

            // 1. Create Donation Record
            var donation = new Donation(
                intentId: intent.Id,
                donorId: userId,
                requestId: intent.RequestId,
                assistanceAmount: intent.AssistanceAmount,
                tipAmount: intent.TipAmount
            );

            _dbContext.Donations.Add(donation);
            await _dbContext.SaveChangesAsync();

            // 2. Create PaymentTransaction Record
            var paymentTx = new PaymentTransaction(donation.Id, "Simulated", dto.GatewayOrderId);
            paymentTx.MarkSuccess(mockPaymentId);
            _dbContext.PaymentTransactions.Add(paymentTx);

            // 3. Update Request Funding Balance & State
            intent.Request.AddContribution(intent.AssistanceAmount);

            // 4. If request is now FullyFunded, queue for direct-to-biller payout
            if (intent.Request.Status == RequestStatus.FullyFunded)
            {
                intent.Request.QueueForPayout();
                var payout = new PayoutRecord(intent.Request.Id, intent.Request.TargetAmount, initiatedByUserId: "system-auto-queue");
                _dbContext.Payouts.Add(payout);
            }

            // 5. Post Double-Entry Journal Entry
            var journalEntry = new JournalEntry("Donation", donation.Id, $"Contribution for {intent.Request.Title}");
            
            // Debit: 1010-Gateway-Clearing (Gross amount collected)
            journalEntry.AddDebit(ledgerAccountId: 1010, amount: donation.GrossAmount);

            // Credit: 2010-Recipient-Escrow-Liability (Assistance funds held in trust)
            journalEntry.AddCredit(ledgerAccountId: 2010, amount: donation.AssistanceAmount);

            // Credit: 4010-Voluntary-Tips (Platform revenue, if any)
            if (donation.PlatformTipAmount > 0)
            {
                journalEntry.AddCredit(ledgerAccountId: 4010, amount: donation.PlatformTipAmount);
            }

            // Validate that sum of debits == sum of credits
            journalEntry.ValidateBalance();

            _dbContext.JournalEntries.Add(journalEntry);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Broadcast real-time funding progress via SignalR
            var supporterCount = await _dbContext.Donations.CountAsync(d => d.RequestId == intent.RequestId && d.Status == DonationStatus.Confirmed);
            var broadcast = new ContributionBroadcastDto(
                RequestId: intent.RequestId,
                ContributionAmount: donation.AssistanceAmount,
                NewFundedAmount: intent.Request.FundedAmount,
                ProgressPercentage: intent.Request.ProgressPercentage,
                SupporterCount: supporterCount,
                Status: intent.Request.Status.ToString()
            );

            await _fundingHub.Clients.All.SendAsync("ContributionReceived", broadcast);
            await _fundingHub.Clients.Group($"request_{intent.RequestId}").SendAsync("RequestUpdated", broadcast);

            var receiptDto = new DonationReceiptDto
            {
                DonationId = donation.Id,
                RequestId = intent.RequestId,
                RequestTitle = intent.Request.Title,
                Amount = donation.AssistanceAmount,
                TipAmount = donation.PlatformTipAmount,
                TotalPaid = donation.GrossAmount,
                Currency = donation.Currency,
                PaidUtc = donation.ConfirmedUtc,
                GatewayPaymentId = mockPaymentId,
                Status = "Confirmed"
            };

            return Ok(ApiResponse<DonationReceiptDto>.Ok(receiptDto, "Contribution confirmed and verified successfully."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse<DonationReceiptDto>.Fail(ex.Message));
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault()
                        ?? Request.Headers["X-Webhook-Signature"].FirstOrDefault()
                        ?? "";

        var secret = _configuration["Payments:WebhookSecret"] ?? "hb_whsec_simulated_development_secret_2026";

        var isValidSignature = await _paymentProvider.VerifyWebhookSignatureAsync(rawPayload, signature, secret);
        if (!isValidSignature)
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid webhook cryptographic signature."));
        }

        // Webhook log and idempotency check
        var eventId = Request.Headers["X-Event-Id"].FirstOrDefault() ?? $"evt_{Guid.NewGuid():N}";
        var existingWebhook = await _dbContext.PaymentWebhooks.FirstOrDefaultAsync(w => w.EventId == eventId);
        if (existingWebhook != null)
        {
            return Ok(ApiResponse<string>.Ok("Webhook event already processed (Idempotent)."));
        }

        var webhookLog = new PaymentWebhook(
            providerName: _paymentProvider.ProviderName,
            eventId: eventId,
            eventType: "payment.captured",
            rawPayload: rawPayload,
            signature: signature
        )
        {
            ProcessedStatus = "Processed"
        };

        _dbContext.PaymentWebhooks.Add(webhookLog);
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Webhook received and processed."));
    }
}

