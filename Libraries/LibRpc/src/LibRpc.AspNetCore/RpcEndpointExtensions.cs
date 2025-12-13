using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Rpc.Server;

namespace Rpc.AspNetCore;

public static class RpcEndpointExtensions
{
    public static IEndpointConventionBuilder MapRpcEndpoint(this WebApplication app, string pattern = "/")
    {
        return app.MapPost(
            pattern,
            async (HttpContext context) =>
            {
                var requestHandler = context.RequestServices.GetRequiredService<RpcRequestHandler>();

                using var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms, context.RequestAborted);

                var packet = Protocol.RpcPacket.FromRawBytes(ms.ToArray());

                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";

                await foreach (var response in requestHandler.HandleAsync(packet, context.RequestAborted))
                {
                    var payload = Convert.ToBase64String(response.AsRawBytes());

                    await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }

                await context.Response.CompleteAsync();
            }
        );
    }
}
