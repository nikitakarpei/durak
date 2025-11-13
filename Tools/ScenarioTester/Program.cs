using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient();

// Register scenario services
builder.Services.AddScoped<ScenarioRunner>();

var app = builder.Build();

// Health check endpoint
app.MapGet(
    "/",
    () =>
        Results.Ok(
            new
            {
                service = "ScenarioTester",
                status = "running",
                endpoints = new string[]
                {
                    "/scenarios",
                    "/scenario/player-login",
                    "/scenario/create-game",
                    "/scenario/full-game-flow",
                },
            }
        )
);

// List all available scenarios
app.MapGet(
    "/scenarios",
    () =>
        Results.Ok(
            new
            {
                scenarios = new object[]
                {
                    new
                    {
                        name = "player-login",
                        description = "Test player login with Android/iOS device ID",
                        endpoint = "/scenario/player-login",
                        parameters = new { deviceId = "optional", platform = "android|ios" },
                    },
                    new
                    {
                        name = "create-game",
                        description = "Create a game with a single player",
                        endpoint = "/scenario/create-game",
                        parameters = new { deviceId = "optional" },
                    },
                    new
                    {
                        name = "full-game-flow",
                        description = "Complete game flow from login to game access",
                        endpoint = "/scenario/full-game-flow",
                        parameters = new object { },
                    },
                },
            }
        )
);

// Scenario 1: Player Login
app.MapPost(
    "/scenario/player-login",
    async (
        [FromServices] ScenarioRunner runner,
        [FromQuery] string? deviceId,
        [FromQuery] string platform = "android"
    ) =>
    {
        try
        {
            var result = await runner.RunPlayerLoginScenario(deviceId, platform);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message, stackTrace = ex.StackTrace }, statusCode: 500);
        }
    }
);

// Scenario 2: Create Game
app.MapPost(
    "/scenario/create-game",
    async ([FromServices] ScenarioRunner runner, [FromQuery] string? deviceId) =>
    {
        try
        {
            var result = await runner.RunCreateGameScenario(deviceId);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message, stackTrace = ex.StackTrace }, statusCode: 500);
        }
    }
);

// Scenario 3: Full Game Flow
app.MapPost(
    "/scenario/full-game-flow",
    async ([FromServices] ScenarioRunner runner) =>
    {
        try
        {
            var result = await runner.RunFullGameFlowScenario();
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message, stackTrace = ex.StackTrace }, statusCode: 500);
        }
    }
);

app.Run();

// Scenario Runner Service
public class ScenarioRunner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ScenarioRunner> _logger;

    public ScenarioRunner(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        ILogger<ScenarioRunner> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    private string GetPlayerIdentityServiceUrl() =>
        _configuration["ServiceUrls:PlayerIdentityService"]
        ?? throw new InvalidOperationException("PlayerIdentityService URL not configured");

    private string GetGameServiceUrl(string playerIdToken)
    {
        var url =
            _configuration["ServiceUrls:GameService"]
            ?? throw new InvalidOperationException("GameService URL not configured");

        return $"{url}?player_id_token={playerIdToken}";
    }

    public async Task<object> RunPlayerLoginScenario(string? deviceId, string platform)
    {
        _logger.LogInformation("Running Player Login Scenario");

        var playerServiceUrl = GetPlayerIdentityServiceUrl();
        deviceId ??= Guid.NewGuid().ToString("N");

        using var httpClient = _httpClientFactory.CreateClient();
        using var connection = new Rpc.Net.Transport.ClientHttpConnection(
            Guid.NewGuid(),
            httpClient,
            new Uri(playerServiceUrl)
        );
        using Rpc.Client.IClient rpcClient = new Rpc.Client.RpcClient(
            connection,
            _loggerFactory.CreateLogger<Rpc.Client.RpcClient>()
        );

        string method;
        string? playerId = null;
        string? token = null;

        if (platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
        {
            method = "LoginWithIosDeviceId";
            var request = new PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request
            {
                DeviceId = deviceId,
            };
            var response = await rpcClient.UnaryCall<
                PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request,
                PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response
            >(method, request, CancellationToken.None);

            playerId = response.Result.PlayerId;
            token = response.Result.PlayerIdToken;
        }
        else
        {
            method = "LoginWithAndroidDeviceId";
            var request = new PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request
            {
                DeviceId = deviceId,
            };
            var response = await rpcClient.UnaryCall<
                PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
                PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response
            >(method, request, CancellationToken.None);

            playerId = response.Result.PlayerId;
            token = response.Result.PlayerIdToken;
        }

        return new
        {
            scenario = "player-login",
            platform,
            deviceId,
            method,
            success = true,
            playerId,
            token,
        };
    }

    public async Task<object> RunCreateGameScenario(string? deviceId)
    {
        _logger.LogInformation("Running Create Game Scenario");

        var steps = new List<object>();

        // Step 1: Login player
        deviceId ??= $"test-device-{Guid.NewGuid():N}";
        var (playerId, token) = await LoginPlayer(deviceId);
        steps.Add(
            new
            {
                step = "login-player",
                deviceId,
                playerId,
                hasToken = !string.IsNullOrEmpty(token),
            }
        );

        // Step 2: Create game
        var gameId = await CreateGame(token);
        steps.Add(new { step = "create-game", gameId });

        return new
        {
            scenario = "create-game",
            success = true,
            gameId,
            playerToken = token,
            steps,
        };
    }

    public async Task<object> RunFullGameFlowScenario()
    {
        _logger.LogInformation("Running Full Game Flow Scenario");

        var steps = new List<object>();

        // Step 1: Login player
        var player1DeviceId = $"test-device-{Guid.NewGuid():N}";
        var (player1Id, player1Token) = await LoginPlayer(player1DeviceId);
        steps.Add(
            new
            {
                step = "login-player",
                deviceId = player1DeviceId,
                playerId = player1Id,
            }
        );

        // Step 2: Create game
        var gameId = await CreateGame(player1Token);
        steps.Add(new { step = "create-game", gameId });

        // Step 3: Get game access token
        var gameAccessToken = await GetGameAccessToken(gameId, player1Token);
        steps.Add(new { step = "get-game-access-token", hasToken = !string.IsNullOrEmpty(gameAccessToken) });

        return new
        {
            scenario = "full-game-flow",
            gameId,
            success = true,
            steps,
        };
    }

    // Helper methods
    private async Task<(string playerId, string token)> LoginPlayer(string deviceId)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        using var connection = new Rpc.Net.Transport.ClientHttpConnection(
            Guid.NewGuid(),
            httpClient,
            new Uri(GetPlayerIdentityServiceUrl())
        );
        using Rpc.Client.IClient rpcClient = new Rpc.Client.RpcClient(
            connection,
            _loggerFactory.CreateLogger<Rpc.Client.RpcClient>()
        );

        var request = new PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request
        {
            DeviceId = deviceId,
        };

        var response = await rpcClient.UnaryCall<
            PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
            PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response
        >("LoginWithAndroidDeviceId", request, CancellationToken.None);

        return (response.Result.PlayerId, response.Result.PlayerIdToken);
    }

    private async Task<string> CreateGame(string playerIdToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        using var connection = new Rpc.Net.Transport.ClientHttpConnection(
            Guid.NewGuid(),
            httpClient,
            new Uri(GetGameServiceUrl(playerIdToken))
        );
        using Rpc.Client.IClient rpcClient = new Rpc.Client.RpcClient(
            connection,
            _loggerFactory.CreateLogger<Rpc.Client.RpcClient>()
        );

        var options = new List<Google.Protobuf.WellKnownTypes.Any>
        {
            Google.Protobuf.WellKnownTypes.Any.Pack(new GameService.Api.Data.GameOptions.AmountOfPlayers { Count = 4 }),
            Google.Protobuf.WellKnownTypes.Any.Pack(
                new GameService.Api.Data.GameOptions.CardRanksInTalon
                {
                    Ranks =
                    {
                        GameService.Api.Data.CardRank.Ace,
                        GameService.Api.Data.CardRank.King,
                        GameService.Api.Data.CardRank.Queen,
                        GameService.Api.Data.CardRank.Jack,
                        GameService.Api.Data.CardRank.Ten,
                    },
                }
            ),
            Google.Protobuf.WellKnownTypes.Any.Pack(new GameService.Api.Data.GameOptions.MinHandSize { Count = 6 }),
        };

        var request = new GameService.Api.Commands.CreateGameCommand.Types.Request { Options = { options } };

        var response = await rpcClient.UnaryCall<
            GameService.Api.Commands.CreateGameCommand.Types.Request,
            GameService.Api.Commands.CreateGameCommand.Types.Response
        >("CreateGame", request, CancellationToken.None);

        if (response.Success != null)
        {
            return response.Success.GameId;
        }

        throw new InvalidOperationException("Failed to create game");
    }

    private async Task<string> GetGameAccessToken(string gameId, string playerIdToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        using var connection = new Rpc.Net.Transport.ClientHttpConnection(
            Guid.NewGuid(),
            httpClient,
            new Uri(GetGameServiceUrl(playerIdToken))
        );
        using Rpc.Client.IClient rpcClient = new Rpc.Client.RpcClient(
            connection,
            _loggerFactory.CreateLogger<Rpc.Client.RpcClient>()
        );

        var request = new GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request { GameId = gameId };

        var response = await rpcClient.UnaryCall<
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request,
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response
        >("GetGameAccessToken", request, CancellationToken.None);

        if (response.Success != null)
        {
            return response.Success.AccessToken;
        }

        throw new InvalidOperationException("Failed to get game access token");
    }
}
