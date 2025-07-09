using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace QuantityTakeoffOrchestratorService.NotificationHubs;

/// <summary>
///     This hub is used to notify clients about events related to quantity takeoff processing.
/// </summary>
public class QuantityTakeoffOrchestratorHub : Hub
{

    public async Task TestMe(string someRandomText)
    {
        await Clients.All.SendAsync(
            $"{this.Context?.User?.Identity?.Name!} : {someRandomText}",
            CancellationToken.None);
    }

    /// <summary>
    ///     Called when a new connection is established with the hub.
    /// </summary>
    //[Trace]
    public override async Task OnConnectedAsync()
    {
        try
        {
            string userId = Context.GetHttpContext().Request.Query["userId"]!;
            string transactionId = Context.GetHttpContext().Request.Query["transactionId"]!;

            var group = string.Concat(userId, "_", transactionId);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            await Clients.Caller.SendAsync("UserConnected", userId, transactionId, group);

            Log.Logger.Information($"UserId: {userId}, TransactionId: {transactionId}, Group: {group} connected via signalr");

            //await this.Clients.Caller.SendAsync("Connected", userId, transactionId);

        }
        catch (Exception ex)
        {
            Log.Logger.Error($"An error occurred for OnConnectedAsync: {ex.Message}");
        }
        finally
        {
            await base.OnConnectedAsync();
        }
    }

    /// <summary>
    ///     Called when a connection with the hub is terminated.
    /// </summary>
    /// <param name="exception"></param>
    //[Trace]
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            string userId = Context.GetHttpContext().Request.Query["userId"]!;
            string transactionId = Context.GetHttpContext().Request.Query["transactionId"]!;

            var group = string.Concat(userId, "_", transactionId);

            Log.Logger.Information($"UserId: {userId}, TransactionId: {transactionId}, Group: {group} disconnected from signalr");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"An error occurred for OnDisconnectedAsync: {ex.Message}");
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
