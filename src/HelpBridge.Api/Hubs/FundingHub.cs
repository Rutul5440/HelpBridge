using Microsoft.AspNetCore.SignalR;

namespace HelpBridge.Api.Hubs;

public class FundingHub : Hub
{
    public async Task SubscribeToRequest(string requestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
    }

    public async Task UnsubscribeFromRequest(string requestId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
    }
}

public record ContributionBroadcastDto(
    Guid RequestId,
    decimal ContributionAmount,
    decimal NewFundedAmount,
    decimal ProgressPercentage,
    int SupporterCount,
    string Status
);
