using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HelpBridge.Application.Common;
using HelpBridge.Application.Features.Donations.DTOs;
using HelpBridge.Application.Features.Identity.DTOs;
using HelpBridge.Application.Features.Requests.DTOs;
using HelpBridge.Application.Features.Verification.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HelpBridge.IntegrationTests;

public class EndToEndWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndToEndWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteUserJourney_RecipientToDonorToPayout_Succeeds()
    {
        // ==========================================
        // 1. RECIPIENT REGISTRATION & LOGIN
        // ==========================================
        var recipientEmail = $"rahul_{Guid.NewGuid():N}@student.edu.in";
        var recipientReg = new RegisterRequestDto
        {
            Email = recipientEmail,
            Password = "Password@1234!",
            ConfirmPassword = "Password@1234!",
            PhoneNumber = "+919876543210",
            FirstName = "Rahul",
            LastName = "Patel",
            Role = "Recipient",
            City = "Ahmedabad",
            State = "Gujarat",
            TermsAccepted = true
        };

        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", recipientReg);
        regRes.IsSuccessStatusCode.Should().BeTrue();
        var regData = await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        regData!.Success.Should().BeTrue();
        var recipientToken = regData.Data!.AccessToken;

        // ==========================================
        // 2. RECIPIENT CREATES DRAFT ASSISTANCE REQUEST
        // ==========================================
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        var createReq = new CreateRequestDto
        {
            CategoryId = 1, // EDUCATION
            Title = "College Semester 6 Tuition Support",
            Story = "Support needed to clear the remaining semester tuition fee balance.",
            TargetAmount = 25000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            BillerName = "Ahmedabad Institute of Technology",
            BillerReferenceNumber = "AIT-2026-REG-4421",
            BillerBankAccountNumber = "998877665544",
            BillerIfscOrCode = "SBIN0001234"
        };

        var createRes = await _client.PostAsJsonAsync("/api/v1/requests", createReq);
        createRes.IsSuccessStatusCode.Should().BeTrue();
        var createdData = await createRes.Content.ReadFromJsonAsync<ApiResponse<AssistanceRequestDto>>();
        createdData!.Success.Should().BeTrue();
        var requestId = createdData.Data!.Id;
        createdData.Data.Status.Should().Be("Draft");

        // ==========================================
        // 3. UPLOAD SUPPORTING DOCUMENT & SUBMIT
        // ==========================================
        var fileContent = new ByteArrayContent("Mock PDF college fee invoice bytes"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        using var multiPartContent = new MultipartFormDataContent();
        multiPartContent.Add(fileContent, "file", "college_fee_invoice.pdf");
        multiPartContent.Add(new StringContent("1"), "documentType"); // FeeReceipt

        var uploadRes = await _client.PostAsync($"/api/v1/requests/{requestId}/documents", multiPartContent);
        uploadRes.IsSuccessStatusCode.Should().BeTrue();

        var submitRes = await _client.PostAsync($"/api/v1/requests/{requestId}/submit", null);
        submitRes.IsSuccessStatusCode.Should().BeTrue();

        // ==========================================
        // 4. ADMIN / VERIFICATION OFFICER APPROVES
        // ==========================================
        var adminLogin = new LoginRequestDto
        {
            Email = "admin@helpbridge.org",
            Password = "HelpBridge@Admin2026!"
        };

        var adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        adminLoginRes.IsSuccessStatusCode.Should().BeTrue();
        var adminData = await adminLoginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        var adminToken = adminData!.Data!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var queueRes = await _client.GetFromJsonAsync<ApiResponse<List<VerificationQueueItemDto>>>("/api/v1/verification/queue");
        queueRes!.Success.Should().BeTrue();
        var caseItem = queueRes.Data!.FirstOrDefault(c => c.RequestId == requestId);
        caseItem.Should().NotBeNull();

        // Officer reviews and approves
        var decisionDto = new VerificationDecisionDto
        {
            CaseId = caseItem!.CaseId,
            Decision = "Approve",
            Notes = "Verified directly with college registrar fee desk. Document matches student claim."
        };

        var decisionRes = await _client.PostAsJsonAsync($"/api/v1/verification/{caseItem.CaseId}/decision", decisionDto);
        decisionRes.IsSuccessStatusCode.Should().BeTrue();

        // ==========================================
        // 5. PUBLIC DISCOVERY FEED VERIFICATION
        // ==========================================
        _client.DefaultRequestHeaders.Authorization = null; // Public unauthenticated request

        var feedRes = await _client.GetFromJsonAsync<ApiResponse<List<RequestSummaryDto>>>("/api/v1/discovery/feed");
        feedRes!.Success.Should().BeTrue();
        var feedItem = feedRes.Data!.FirstOrDefault(f => f.Id == requestId);
        feedItem.Should().NotBeNull();
        feedItem!.TargetAmount.Should().Be(25000m);
        feedItem.IsVerified.Should().BeTrue();

        // ==========================================
        // 6. DONOR REGISTRATION & CONTRIBUTION
        // ==========================================
        var donorEmail = $"james_{Guid.NewGuid():N}@philanthropy.org";
        var donorReg = new RegisterRequestDto
        {
            Email = donorEmail,
            Password = "Password@1234!",
            ConfirmPassword = "Password@1234!",
            PhoneNumber = "+919876543211",
            FirstName = "James",
            LastName = "Wilson",
            Role = "Donor",
            City = "Mumbai",
            State = "Maharashtra",
            TermsAccepted = true
        };

        var donorRegRes = await _client.PostAsJsonAsync("/api/v1/auth/register", donorReg);
        donorRegRes.IsSuccessStatusCode.Should().BeTrue();
        var donorData = await donorRegRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        var donorToken = donorData!.Data!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", donorToken);

        // Donor creates donation intent (₹25,000 full target + ₹1,000 voluntary tip)
        var intentDto = new CreateDonationIntentDto
        {
            RequestId = requestId,
            AssistanceAmount = 25000m,
            PlatformTipAmount = 1000m,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var intentRes = await _client.PostAsJsonAsync("/api/v1/donations/intents", intentDto);
        intentRes.IsSuccessStatusCode.Should().BeTrue();
        var intentData = await intentRes.Content.ReadFromJsonAsync<ApiResponse<DonationIntentResponseDto>>();
        intentData!.Success.Should().BeTrue();

        // Complete simulated payment
        var captureDto = new SimulatedPaymentCaptureDto
        {
            IntentId = intentData.Data!.IntentId,
            GatewayOrderId = intentData.Data.GatewayOrderId
        };

        var captureRes = await _client.PostAsJsonAsync("/api/v1/payments/simulated/capture", captureDto);
        captureRes.IsSuccessStatusCode.Should().BeTrue();
        var receipt = await captureRes.Content.ReadFromJsonAsync<ApiResponse<DonationReceiptDto>>();
        receipt!.Success.Should().BeTrue();
        receipt.Data!.Amount.Should().Be(25000m);
        receipt.Data.TipAmount.Should().Be(1000m);
        receipt.Data.Status.Should().Be("Confirmed");

        // ==========================================
        // 7. VERIFY ADMIN TELEMETRY & LEDGER BALANCE
        // ==========================================
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var metricsRes = await _client.GetFromJsonAsync<ApiResponse<HelpBridge.Api.Controllers.v1.AdminDashboardMetricsDto>>("/api/v1/admin/dashboard");
        metricsRes!.Success.Should().BeTrue();
        metricsRes.Data!.TotalContributionsFacilitated.Should().BeGreaterThanOrEqualTo(25000m);
        metricsRes.Data.TotalPlatformTipsRevenue.Should().BeGreaterThanOrEqualTo(1000m);
        metricsRes.Data.IsLedgerBalanced.Should().BeTrue("Double-entry accounting invariant (Debits == Credits) must hold");
        metricsRes.Data.LedgerVariance.Should().Be(0m);
    }
}
