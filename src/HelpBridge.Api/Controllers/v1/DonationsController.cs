using System.Security.Claims;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Donations.DTOs;
using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Donations;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payments;
using HelpBridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpBridge.Api.Controllers.v1;

[ApiController]
[Route("api/v1/donations")]
public class DonationsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPaymentProvider _paymentProvider;

    public DonationsController(AppDbContext dbContext, IPaymentProvider paymentProvider)
    {
        _dbContext = dbContext;
        _paymentProvider = paymentProvider;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    [HttpPost("intents")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DonationIntentResponseDto>>> CreateDonationIntent([FromBody] CreateDonationIntentDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (dto.AssistanceAmount <= 0)
        {
            return BadRequest(ApiResponse<DonationIntentResponseDto>.Fail("Contribution amount must be greater than zero."));
        }

        // Check for existing intent with the same idempotency key
        var existingIntent = await _dbContext.DonationIntents
            .Include(i => i.Donation)
            .FirstOrDefaultAsync(i => i.IdempotencyKey == dto.IdempotencyKey);

        if (existingIntent != null && !existingIntent.IsExpired)
        {
            var existingTx = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(p => p.DonationId == existingIntent.Id);
            return Ok(ApiResponse<DonationIntentResponseDto>.Ok(new DonationIntentResponseDto
            {
                IntentId = existingIntent.Id,
                GatewayOrderId = existingTx?.GatewayOrderId ?? "pending_order",
                ProviderName = _paymentProvider.ProviderName,
                AssistanceAmount = existingIntent.AssistanceAmount,
                TipAmount = existingIntent.TipAmount,
                TotalAmount = existingIntent.TotalAmount,
                Currency = existingIntent.Currency,
                ExpiresUtc = existingIntent.ExpiresUtc
            }, "Returned existing active donation intent (Idempotent)."));
        }

        var request = await _dbContext.AssistanceRequests.FindAsync(dto.RequestId);
        if (request == null)
        {
            return NotFound(ApiResponse<DonationIntentResponseDto>.Fail("Assistance request not found."));
        }

        if (!request.IsFundable)
        {
            return BadRequest(ApiResponse<DonationIntentResponseDto>.Fail($"This request is currently '{request.Status}' and cannot receive contributions."));
        }

        if (dto.AssistanceAmount > request.RemainingAmount)
        {
            return BadRequest(ApiResponse<DonationIntentResponseDto>.Fail(
                $"Contribution amount ₹{dto.AssistanceAmount:N2} exceeds remaining required amount of ₹{request.RemainingAmount:N2}."
            ));
        }

        var intent = new DonationIntent(
            donorId: userId,
            requestId: dto.RequestId,
            assistanceAmount: dto.AssistanceAmount,
            tipAmount: Math.Max(0m, dto.PlatformTipAmount),
            idempotencyKey: dto.IdempotencyKey
        );

        _dbContext.DonationIntents.Add(intent);
        await _dbContext.SaveChangesAsync();

        // Create Order via Payment Provider (Simulated or Gateway)
        var orderResult = await _paymentProvider.CreateOrderAsync(intent.TotalAmount, intent.Currency, intent.Id.ToString());

        var response = new DonationIntentResponseDto
        {
            IntentId = intent.Id,
            GatewayOrderId = orderResult.OrderId,
            ProviderName = orderResult.ProviderName,
            AssistanceAmount = intent.AssistanceAmount,
            TipAmount = intent.TipAmount,
            TotalAmount = intent.TotalAmount,
            Currency = intent.Currency,
            ExpiresUtc = intent.ExpiresUtc
        };

        return Ok(ApiResponse<DonationIntentResponseDto>.Ok(response, "Donation intent initialized. Proceed to payment checkout."));
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<DonationReceiptDto>>>> GetMyDonationHistory()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var donations = await _dbContext.Donations
            .Include(d => d.Request)
            .Include(d => d.PaymentTransaction)
            .Where(d => d.DonorId == userId && d.Status == DonationStatus.Confirmed)
            .OrderByDescending(d => d.ConfirmedUtc)
            .ToListAsync();

        var dtos = donations.Select(d => new DonationReceiptDto
        {
            DonationId = d.Id,
            RequestId = d.RequestId,
            RequestTitle = d.Request.Title,
            Amount = d.AssistanceAmount,
            TipAmount = d.PlatformTipAmount,
            TotalPaid = d.GrossAmount,
            Currency = d.Currency,
            PaidUtc = d.ConfirmedUtc,
            GatewayPaymentId = d.PaymentTransaction?.GatewayPaymentId ?? "COMPLETED",
            Status = d.Status.ToString()
        }).ToList();

        return Ok(ApiResponse<List<DonationReceiptDto>>.Ok(dtos));
    }
}

