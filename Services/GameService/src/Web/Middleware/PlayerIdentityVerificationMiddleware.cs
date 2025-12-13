using GameService.Application;
using PlayerIdentityVerify;

namespace GameService.Web.Middleware;

public class PlayerIdentityVerificationMiddleware(
    IPlayerIdentityTokenVerifier playerIdentityTokenVerifier,
    VerifiedPlayerIdentityAccessor verifiedPlayerIdentityAccessor
) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Query.TryGetValue("player_id_token", out var playerIdToken))
        {
            var playerIdentity = await playerIdentityTokenVerifier.VerifyAsync(playerIdToken!, context.RequestAborted);

            if (playerIdentity is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Player identity verification failed", context.RequestAborted);
                return;
            }

            verifiedPlayerIdentityAccessor.Identity = playerIdentity;
        }

        await next(context);
    }
}
