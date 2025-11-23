var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// RPC Configuration
Rpc.Server.CallHandlerRegistryBuilder callHandlerRegistryBuilder = Rpc.Server.CallHandlerRegistry.CreateBuilder();

// TODO: Add RPC call handlers

builder.Services.AddSingleton(callHandlerRegistryBuilder.Build());

builder.Services.AddScoped<Rpc.Server.RpcRequestHandler>();

var app = builder.Build();

app.MapPost(
    "/",
    async context =>
    {
        var requestHandler = context.RequestServices.GetRequiredService<Rpc.Server.RpcRequestHandler>();

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
);

app.Run();
