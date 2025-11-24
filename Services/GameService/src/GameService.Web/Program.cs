using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using EventStore.MongoDb;
using Google.Api.Gax;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Rpc.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Infrastructure Services
builder.Services.AddHttpClient();

// GCP Configuration
string gcpProjectId =
    Platform.Instance().ProjectId
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
    ?? throw new InvalidOperationException(
        "No GCP project ID found. Set GOOGLE_CLOUD_PROJECT locally or run `gcloud auth application-default login`."
    );

// Secrets Provider
builder.Services.AddSingleton<Secrets.ISecretsProvider>(sp => new Secrets.Gcp.GoogleSecretManagerProvider(
    Google.Cloud.SecretManager.V1.SecretManagerServiceClient.Create(),
    gcpProjectId
));

// MongoDB Configuration
var mongoDbOptions =
    builder.Configuration.GetSection("MongoDb").Get<MongoDbOptions>() ?? throw new Exception(
        "MongoDbOptions is not configured"
    );

var mongoClient = new MongoClient(mongoDbOptions.ConnectionString);

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IMongoDatabase>(sp => mongoClient.GetDatabase(mongoDbOptions.DatabaseName));

// Event Store
builder.Services.AddMongoDbEventStore(options =>
{
    options.CollectionName = "eventStreams";
});

// Akka.NET Configuration
var akkaConfig = File.ReadAllText("akka.conf");

builder.Services.AddSingleton<ActorSystem>(sp =>
{
    var config = ConfigurationFactory.ParseString(akkaConfig);
    var bootstrap = BootstrapSetup.Create().WithConfig(config);
    var di = DependencyResolverSetup.Create(sp);
    var actorSystemSetup = bootstrap.And(di);

    return ActorSystem.Create("GameService", actorSystemSetup);
});
builder.Services.AddSingleton<IActorRefFactory>(sp => sp.GetRequiredService<ActorSystem>());
builder.Services.AddSingleton<IDependencyResolver>(sp =>
{
    var system = sp.GetRequiredService<ActorSystem>();
    return DependencyResolver.For(system).Resolver;
});

// MediatR Configuration
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GameService.Application.GameDomainEventNotification<>).Assembly);
    cfg.RegisterGenericHandlers = true;
});

// Player Identity Verification
string playerIdentityServiceUrl =
    Environment.GetEnvironmentVariable("PLAYER_IDENTITY_SERVICE_URL")
    ?? throw new InvalidOperationException("No Player Identity Service URL found in environment variables.");

builder.Services.AddSingleton<
    PlayerIdentityVerify.IPlayerIdentityServiceClientFactory,
    PlayerIdentityVerify.Net.HttpPlayerIdentityServiceClientFactory
>();
builder.Services.AddSingleton<PlayerIdentityVerify.IPlayerIdentityTokenVerifier>(
    sp => new PlayerIdentityVerify.PlayerIdentityTokenVerifier(
        sp.GetRequiredService<PlayerIdentityVerify.IPlayerIdentityServiceClientFactory>(),
        playerIdentityServiceUrl
    )
);

builder.Services.AddScoped<GameService.Application.VerifiedPlayerIdentityAccessor>();
builder.Services.AddScoped<GameService.Application.IVerifiedPlayerIdentityProvider>(sp =>
    sp.GetRequiredService<GameService.Application.VerifiedPlayerIdentityAccessor>()
);

// Game Domain Services
builder.Services.AddSingleton<GameService.Application.IGameCollection, GameService.Infrastructure.GameCollection>();
builder.Services.AddSingleton<
    GameService.Infrastructure.IRandomNumberGenerator,
    GameService.Infrastructure.RandomNumberGenerator
>();
builder.Services.AddSingleton<GameService.Domain.IPossibleMovesFinder, GameService.Domain.PossibleMovesFinder>();
builder.Services.AddSingleton<
    GameService.Domain.IShuffledPositionsGenerator,
    GameService.Infrastructure.ShuffledPositionsGenerator
>();
builder.Services.AddSingleton<GameService.Domain.IAttackerFinder, GameService.Domain.AttackerFinder>();
builder.Services.AddSingleton<GameService.Domain.IDefenderFinder, GameService.Domain.DefenderFinder>();
builder.Services.AddSingleton<GameService.Domain.ICardMoveValidator, GameService.Domain.CardMoveValidator>();
builder.Services.AddSingleton<GameService.Domain.ITalonCreator, GameService.Domain.TalonCreator>();
builder.Services.AddSingleton<GameService.Domain.IFoolFinder, GameService.Domain.FoolFinder>();
builder.Services.AddSingleton<GameService.Domain.GameService>();

// Authentication & Authorization
builder.Services.AddSingleton<GameService.Infrastructure.GameAccessTokenService>();
builder.Services.AddSingleton<GameService.Application.IGameAccessTokenIssuer>(sp =>
    sp.GetRequiredService<GameService.Infrastructure.GameAccessTokenService>()
);
builder.Services.AddSingleton<GameService.Application.IGameAccessTokenVerifier>(sp =>
    sp.GetRequiredService<GameService.Infrastructure.GameAccessTokenService>()
);

builder.Services.Configure<GameService.Infrastructure.GameAccessTokenServiceOptions>(
    builder.Configuration.GetSection("GameAccessTokenServiceOptions")
);

// Command Handlers
builder.Services.AddScoped<GameService.Api.CreateGameCommandHandler>();
builder.Services.AddScoped<GameService.Api.JoinLobbyCommandHandler>();
builder.Services.AddScoped<GameService.Api.PlayAttackCardCommandHandler>();
builder.Services.AddScoped<GameService.Api.BeatAttackCardCommandHandler>();
builder.Services.AddScoped<GameService.Api.GiveUpDefenceCommandHandler>();
builder.Services.AddScoped<GameService.Api.GetGameAccessTokenCommandHandler>();
builder.Services.AddScoped<GameService.Api.GetGameEventsCommandHandler>();

// RPC Configuration
Rpc.Server.CallHandlerRegistryBuilder callHandlerRegistryBuilder = Rpc.Server.CallHandlerRegistry.CreateBuilder();

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.CreateGameCommand.Types.Request,
    GameService.Api.Commands.CreateGameCommand.Types.Response,
    GameService.Api.CreateGameCommandHandler
>("CreateGame");

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.JoinLobbyCommand.Types.Request,
    GameService.Api.Commands.JoinLobbyCommand.Types.Response,
    GameService.Api.JoinLobbyCommandHandler
>("JoinLobby");

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.PlayAttackCardCommand.Types.Request,
    GameService.Api.Commands.PlayAttackCardCommand.Types.Response,
    GameService.Api.PlayAttackCardCommandHandler
>("PlayAttackCard");

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.BeatAttackCardCommand.Types.Request,
    GameService.Api.Commands.BeatAttackCardCommand.Types.Response,
    GameService.Api.BeatAttackCardCommandHandler
>("BeatAttackCard");

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.GiveUpDefenceCommand.Types.Request,
    GameService.Api.Commands.GiveUpDefenceCommand.Types.Response,
    GameService.Api.GiveUpDefenceCommandHandler
>("GiveUpDefence");

callHandlerRegistryBuilder.AddUnaryCall<
    GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request,
    GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response,
    GameService.Api.GetGameAccessTokenCommandHandler
>("GetGameAccessToken");

callHandlerRegistryBuilder.AddStreamingCall<
    GameService.Api.Commands.GetGameEventsCommand.Types.Request,
    GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse,
    GameService.Api.GetGameEventsCommandHandler
>("GetGameEvents");

builder.Services.AddSingleton(callHandlerRegistryBuilder.Build());

builder.Services.AddScoped<Rpc.Server.RpcRequestHandler>();
builder.Services.AddScoped<GameService.Web.Middleware.PlayerIdentityVerificationMiddleware>();

// Application Build & Configuration
var app = builder.Build();

app.UseMiddleware<GameService.Web.Middleware.PlayerIdentityVerificationMiddleware>();
app.MapRpcEndpoint("/");

app.Run();

class MongoDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
