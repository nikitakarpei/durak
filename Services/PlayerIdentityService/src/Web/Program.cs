using Google.Api.Gax;
using MongoDB.Driver;
using Rpc.AspNetCore;

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

builder.Services.AddScoped<PlayerIdentityService.BuildingBlocks.Infrastructure.MongoSessionAccessor>();

builder.Services.AddScoped<PlayerIdentityService.BuildingBlocks.Infrastructure.IMongoSessionProvider>(sp =>
    sp.GetRequiredService<PlayerIdentityService.BuildingBlocks.Infrastructure.MongoSessionAccessor>()
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Modules.Players.Infrastructure.Persistence.Models.MongoDb.PlayerModel>(
        "players"
    )
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Modules.Auth.AndroidDeviceId.Infrastructure.Persistence.Models.MongoDb.AndroidDeviceIdLinkModel>(
        "androidDeviceIdLinks"
    )
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Modules.Auth.IosDeviceId.Infrastructure.Persistence.Models.MongoDb.IosDeviceIdLinkModel>(
        "iosDeviceIdLinks"
    )
);

builder.Services.AddScoped(_ =>
    database.GetCollection<PlayerIdentityService.Modules.Auth.CustomId.Infrastructure.Persistence.Models.MongoDb.CustomIdLinkModel>(
        "customIdLinks"
    )
);

builder.Services.Configure<PlayerIdentityService.Modules.Players.Infrastructure.Authentication.PlayerIdentityTokenIssuerOptions>(
    builder.Configuration.GetSection("PlayerIdentityTokenIssuerOptions")
);

builder.Services.AddScoped<
    PlayerIdentityService.Modules.Players.Application.Authentication.IPlayerIdentityTokenIssuer,
    PlayerIdentityService.Modules.Players.Infrastructure.Authentication.PlayerIdentityTokenIssuer
>();

builder.Services.AddScoped<
    PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Repositories.IAndroidDeviceIdLinkRepository,
    PlayerIdentityService.Modules.Auth.AndroidDeviceId.Infrastructure.Persistence.Repositories.MongoDb.AndroidDeviceIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Repositories.IIosDeviceIdLinkRepository,
    PlayerIdentityService.Modules.Auth.IosDeviceId.Infrastructure.Persistence.Repositories.MongoDb.IosDeviceIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Modules.Auth.CustomId.Application.Repositories.ICustomIdLinkRepository,
    PlayerIdentityService.Modules.Auth.CustomId.Infrastructure.Persistence.Repositories.MongoDb.CustomIdLinkRepository
>();
builder.Services.AddScoped<
    PlayerIdentityService.Modules.Players.Application.Repositories.IPlayerRepository,
    PlayerIdentityService.Modules.Players.Infrastructure.Persistence.Repositories.MongoDb.PlayerRepository
>();

builder.Services.AddScoped<PlayerIdentityService.Modules.Players.Application.Services.PlayerService>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Services.AndroidDeviceIdLinkService>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Services.IosDeviceIdLinkService>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.CustomId.Application.Services.CustomIdLinkService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(PlayerIdentityService.Modules.Players.Application.Handlers.CreatePlayerCommandHandler).Assembly
    );
});

builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.AndroidDeviceId.Api.CommandHandlers.LoginWithAndroidDeviceIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.IosDeviceId.Api.CommandHandlers.LoginWithIosDeviceIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Auth.CustomId.Api.CommandHandlers.LoginWithCustomIdCommandHandler>();
builder.Services.AddScoped<PlayerIdentityService.Modules.Players.Api.CommandHandlers.GetIdentityTokenPublicKeyCommandHandler>();

Rpc.Server.CallHandlerRegistryBuilder callHandlerRegistryBuilder = Rpc.Server.CallHandlerRegistry.CreateBuilder();

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response,
    PlayerIdentityService.Modules.Auth.AndroidDeviceId.Api.CommandHandlers.LoginWithAndroidDeviceIdCommandHandler
>("LoginWithAndroidDeviceId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response,
    PlayerIdentityService.Modules.Auth.IosDeviceId.Api.CommandHandlers.LoginWithIosDeviceIdCommandHandler
>("LoginWithIosDeviceId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request,
    PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response,
    PlayerIdentityService.Modules.Auth.CustomId.Api.CommandHandlers.LoginWithCustomIdCommandHandler
>("LoginWithCustomId");

callHandlerRegistryBuilder.AddUnaryCall<
    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response,
    PlayerIdentityService.Modules.Players.Api.CommandHandlers.GetIdentityTokenPublicKeyCommandHandler
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

app.MapRpcEndpoint("/");

app.Run();

class MongoDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
