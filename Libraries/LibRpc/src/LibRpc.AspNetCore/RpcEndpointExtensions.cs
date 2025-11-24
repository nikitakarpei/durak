using System.Buffers.Binary;
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

                using var reader = new MemoryStream(new byte[context.Request.ContentLength ?? 0]);
                await context.Request.Body.CopyToAsync(reader, context.RequestAborted);

                var packet = Protocol.RpcPacket.FromRawBytes(reader.ToArray());

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/octet-stream";

                await foreach (var response in requestHandler.HandleAsync(packet, context.RequestAborted))
                {
                    var responseBytes = response.AsRawBytes();
                    var lengthPrefix = new byte[4];
                    BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, responseBytes.Length);

                    await context.Response.Body.WriteAsync(lengthPrefix, context.RequestAborted);
                    await context.Response.Body.WriteAsync(responseBytes, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }

                await context.Response.CompleteAsync();
            }
        );
    }
}
