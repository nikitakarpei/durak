namespace GameService.Web.Endpoints;

internal class RpcEndpointHandler(
    Rpc.Server.RpcRequestHandler requestHandler,
    PlayerIdentityVerify.IPlayerIdentityTokenVerifier playerIdentityTokenVerifier,
    Application.VerifiedPlayerIdentityAccessor verifiedPlayerIdentityAccessor
)
{
    public async Task HandleAsync(HttpContext context)
    {
        if (!await VerifyPlayerIdentityAsync(context))
        {
            return;
        }

        await HandleHttpRequestAsync(context);
    }

    private async Task<bool> VerifyPlayerIdentityAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("player_id_token", out var playerIdToken))
        {
            var playerIdentity = await playerIdentityTokenVerifier.VerifyAsync(playerIdToken!, context.RequestAborted);

            if (playerIdentity is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Player identity verification failed", context.RequestAborted);

                return false;
            }

            verifiedPlayerIdentityAccessor.Identity = playerIdentity;
        }

        return true;
    }

    private async Task HandleHttpRequestAsync(HttpContext context)
    {
        using var reader = new MemoryStream(new byte[context.Request.ContentLength ?? 0]);
        await context.Request.Body.CopyToAsync(reader, context.RequestAborted);

        var packet = Rpc.Protocol.RpcPacket.FromRawBytes(reader.ToArray());

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/octet-stream";

        await foreach (var response in requestHandler.HandleAsync(packet, context.RequestAborted))
        {
            await context.Response.Body.WriteAsync(response.AsRawBytes(), context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }

        await context.Response.CompleteAsync();
    }
}
