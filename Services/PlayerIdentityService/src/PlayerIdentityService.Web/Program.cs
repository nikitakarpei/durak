using System.Buffers.Binary;
using Google.Api.Gax;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

string gcpProjectId =
    Platform.Instance().ProjectId
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
    ?? throw new InvalidOperationException(
        "No GCP project ID found. Set GOOGLE_CLOUD_PROJECT locally or run `gcloud auth application-default login`."
    );

builder.Services.AddSingleton<Secrets.ISecretsProvider>(sp => new Secrets.Gcp.GoogleSecretManagerProvider(
    Google.Cloud.SecretManager.V1.SecretManagerServiceClient.Create(),
    gcpProjectId
));

var mongoDbOptions =
    builder.Configuration.GetSection("MongoDb").Get<MongoDbOptions>() ?? throw new Exception(
        "MongoDbOptions is not configured"
    );

var mongoClient = new MongoClient(mongoDbOptions.ConnectionString);

builder.Services.AddSingleton<IMongoClient>(mongoClient);

var database = mongoClient.GetDatabase(mongoDbOptions.DatabaseName);

builder.Services.AddScoped<PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb.MongoSessionAccessor>();

builder.Services.AddScoped<PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb.IMongoSessionProvider>(
    sp =>
        sp.GetRequiredService<PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb.MongoSessionAccessor>()
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Infrastructure.Persistence.Models.MongoDb.PlayerModel>("players")
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Infrastructure.Persistence.Models.MongoDb.AndroidDeviceIdLinkModel>(
        "androidDeviceIdLinks"
    )
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Infrastructure.Persistence.Models.MongoDb.IosDeviceIdLinkModel>(
        "iosDeviceIdLinks"
    )
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Infrastructure.Persistence.Models.MongoDb.CustomIdLinkModel>(
        "customIdLinks"
    )
);

builder.Services.Configure<PlayerIdentityService.Infrastructure.Authentication.PlayerIdentityTokenIssuerOptions>(
    builder.Configuration.GetSection("PlayerIdentityTokenIssuerOptions")
);

builder.Services.AddScoped<
    PlayerIdentityService.Application.IPlayerIdentityTokenIssuer,
    PlayerIdentityService.Infrastructure.Authentication.PlayerIdentityTokenIssuer
>();

builder.Services.AddScoped<
    PlayerIdentityService.Application.IAndroidDeviceIdLinkRepository,
    PlayerIdentityService.Infrastructure.Persistence.Repositories.MongoDb.AndroidDeviceIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Application.IIosDeviceIdLinkRepository,
    PlayerIdentityService.Infrastructure.Persistence.Repositories.MongoDb.IosDeviceIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Application.ICustomIdLinkRepository,
    PlayerIdentityService.Infrastructure.Persistence.Repositories.MongoDb.CustomIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Application.IPlayerRepository,
    PlayerIdentityService.Infrastructure.Persistence.Repositories.MongoDb.PlayerRepository
>();

builder.Services.AddScoped<PlayerIdentityService.Application.PlayerService>();
builder.Services.AddScoped<PlayerIdentityService.Application.AndroidDeviceIdLinkService>();
builder.Services.AddScoped<PlayerIdentityService.Application.IosDeviceIdLinkService>();
builder.Services.AddScoped<PlayerIdentityService.Application.CustomIdLinkService>();

builder.Services.AddScoped<PlayerIdentityService.Api.LoginWithAndroidDeviceIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Api.LoginWithIosDeviceIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Api.LoginWithCustomIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Api.GetIdentityTokenPublicKeyCommandHandler>();

Rpc.Server.CallHandlerRegistryBuilder callHandlerRegistryBuilder = Rpc.Server.CallHandlerRegistry.CreateBuilder();

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response,
    PlayerIdentityService.Api.LoginWithAndroidDeviceIdCommandHandler
>("LoginWithAndroidDeviceId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response,
    PlayerIdentityService.Api.LoginWithIosDeviceIdCommandHandler
>("LoginWithIosDeviceId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response,
    PlayerIdentityService.Api.LoginWithCustomIdCommandHandler
>("LoginWithCustomId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response,
    PlayerIdentityService.Api.GetIdentityTokenPublicKeyCommandHandler
>("GetIdentityTokenPublicKey");

builder.Services.AddSingleton(callHandlerRegistryBuilder.Build());

builder.Services.AddSingleton<Rpc.Server.IUnaryCallInterceptor>(
    sp => new PlayerIdentityService.Web.MongoDb.UnaryMongoDbTransactionInterceptor(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<ILogger<PlayerIdentityService.Web.MongoDb.UnaryMongoDbTransactionInterceptor>>()
    )
);

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

app.Run();

class MongoDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
