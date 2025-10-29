namespace GameService.Web.Endpoints;

internal class RpcEndpointHandler(
    Rpc.Server.RpcRequestHandler requestHandler,
    PlayerIdentityVerify.IPlayerIdentityTokenVerifier playerIdentityTokenVerifier,
    Application.VerifiedPlayerIdentityAccessor verifiedPlayerIdentityAccessor
)
{
    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("websocket request is required", context.RequestAborted);
            return;
        }

        if (!context.Request.Query.TryGetValue("player_id_token", out var playerIdToken))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("player_id_token is required", context.RequestAborted);
            return;
        }

        var playerIdentity = await playerIdentityTokenVerifier.VerifyAsync(playerIdToken!, context.RequestAborted);

        if (playerIdentity is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("invalid player_id_token", context.RequestAborted);
            return;
        }

        verifiedPlayerIdentityAccessor.Identity = playerIdentity;

        await HandleWebSocketAsync(context, context.RequestAborted);
    }

    private async Task HandleWebSocketAsync(HttpContext context, CancellationToken cancellationToken)
    {
        using var webSocketAdapter = new Adapters.WebSocketAdapter(await context.WebSockets.AcceptWebSocketAsync());

        webSocketAdapter.MessageReceived += async message =>
        {
            var packet = Rpc.Protocol.RpcPacket.FromRawBytes(message);

            await foreach (var response in requestHandler.HandleAsync(packet, cancellationToken))
            {
                await webSocketAdapter.SendAsync(response.AsRawBytes(), cancellationToken);
            }
        };

        await webSocketAdapter.PollAsync(cancellationToken);
    }
}
