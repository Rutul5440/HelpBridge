using System.Security.Cryptography;
using System.Text;
using HelpBridge.Application.Interfaces;

namespace HelpBridge.Infrastructure.Payments;

public class SimulatedPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Simulated";

    public Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receiptId, CancellationToken cancellationToken = default)
    {
        var mockOrderId = $"order_sim_{Guid.NewGuid():N}";
        var result = new PaymentOrderResult(mockOrderId, ProviderName, amount, currency);
        return Task.FromResult(result);
    }

    public Task<bool> VerifyWebhookSignatureAsync(string rawPayload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(rawPayload) || string.IsNullOrWhiteSpace(signature))
            return Task.FromResult(false);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));
        var expectedSignature = Convert.ToHexStringLower(hash);

        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant())
        );

        return Task.FromResult(isValid);
    }

    public static string GenerateSignature(string rawPayload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));
        return Convert.ToHexStringLower(hash);
    }
}

