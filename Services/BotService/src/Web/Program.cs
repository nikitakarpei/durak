using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using Rpc.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// HTTP Client
builder.Services.AddHttpClient();

// Configure options
builder.Services.Configure<BotService.Infrastructure.GameServiceIntegration.GameServiceOptions>(
    builder.Configuration.GetSection("GameService")
);
builder.Services.Configure<BotService.Infrastructure.PlayerIdentityServiceIntegration.PlayerIdentityServiceOptions>(
    builder.Configuration.GetSection("PlayerIdentityService")
);

// Akka.NET Configuration
var akkaConfig = File.ReadAllText("akka.conf");

builder.Services.AddSingleton<ActorSystem>(sp =>
{
    var config = ConfigurationFactory.ParseString(akkaConfig);
    var bootstrap = BootstrapSetup.Create().WithConfig(config);
    var di = DependencyResolverSetup.Create(sp);
    var actorSystemSetup = bootstrap.And(di);

    return ActorSystem.Create("BotService", actorSystemSetup);
});
builder.Services.AddSingleton<IActorRefFactory>(sp => sp.GetRequiredService<ActorSystem>());
builder.Services.AddSingleton<IDependencyResolver>(sp =>
{
    var system = sp.GetRequiredService<ActorSystem>();
    return DependencyResolver.For(system).Resolver;
});

// Register factories
builder.Services.AddScoped<
    BotService.Application.GameServiceIntegration.IRpcClientFactory,
    BotService.Infrastructure.GameServiceIntegration.RpcClientFactory
>();
builder.Services.AddScoped<
    BotService.Application.PlayerIdentityServiceIntegration.IRpcClientFactory,
    BotService.Infrastructure.PlayerIdentityServiceIntegration.RpcClientFactory
>();

builder.Services.AddSingleton<BotService.Application.Bot.IBotCreator, BotService.Infrastructure.Bot.BotCreator>();

builder.Services.AddScoped<BotService.Api.CommandHandlers.CreateBotCommandHandler>();

// RPC Configuration
Rpc.Server.CallHandlerRegistryBuilder callHandlerRegistryBuilder = Rpc.Server.CallHandlerRegistry.CreateBuilder();

callHandlerRegistryBuilder.AddUnaryCall<
    BotService.Api.Commands.CreateBotCommand.Types.Request,
    BotService.Api.Commands.CreateBotCommand.Types.Response,
    BotService.Api.CommandHandlers.CreateBotCommandHandler
>("CreateBot");

builder.Services.AddSingleton(callHandlerRegistryBuilder.Build());

builder.Services.AddScoped<Rpc.Server.RpcRequestHandler>();

var app = builder.Build();

app.MapRpcEndpoint("/");

app.Run();
