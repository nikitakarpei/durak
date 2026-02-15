using BlazorPWA.Configuration;
using BlazorPWA.Services.Api;
using BlazorPWA.Services.Rpc;
using BlazorPWA.State;
using GameService.Api.Commands;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace BlazorPWA.Services.Connection;

public sealed class GameConnectionService : IGameConnectionService
{
    private readonly RpcClientFactory _rpcClientFactory;
    private readonly AppSettings _settings;
    private readonly GameState _state;

    private PlayerIdentityServiceClient? _identityClient;
    private GameServiceClient? _gameClient;
    private CancellationTokenSource? _streamCts;
    private string? _accessToken;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Idle;
    public string? ErrorMessage { get; private set; }
    public string? AccessToken => _accessToken;

    public event Action? StateChanged;

    public GameConnectionService(RpcClientFactory rpcClientFactory, IOptions<AppSettings> options, GameState state)
    {
        _rpcClientFactory = rpcClientFactory;
        _settings = options.Value;
        _state = state;
    }

    public async Task ConnectAsync(string gameId, string customId, CancellationToken cancellationToken)
    {
        ResetError();
        Status = ConnectionStatus.Connecting;
        NotifyStateChanged();

        try
        {
            _state.GameId = gameId;

            var identityEndpoint = new Uri(_settings.Services.PlayerIdentityServiceUrl);
            _identityClient = new PlayerIdentityServiceClient(_rpcClientFactory.Create(identityEndpoint));

            var effectiveCustomId = string.IsNullOrWhiteSpace(customId) ? _settings.Auth.CustomId : customId;
            var loginResult = await _identityClient.LoginWithCustomIdAsync(effectiveCustomId, cancellationToken);
            _state.LocalPlayerId = loginResult.PlayerId;

            var gameEndpoint = BuildGameServiceEndpoint(loginResult.PlayerIdToken);
            _gameClient = new GameServiceClient(_rpcClientFactory.Create(gameEndpoint));

            var accessToken = await _gameClient.GetGameAccessTokenAsync(gameId, cancellationToken);
            _accessToken = accessToken;

            _streamCts?.Cancel();
            _streamCts = new CancellationTokenSource();

            _ = Task.Run(() => ListenForEventsAsync(gameId, _streamCts.Token), _streamCts.Token);

            await _gameClient.JoinLobbyAsync(accessToken, cancellationToken);

            Status = ConnectionStatus.Connected;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _state.GameId = string.Empty;
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    public async Task PlayAttackCardAsync(string accessToken, string cardId, CancellationToken cancellationToken)
    {
        try
        {
            EnsureGameClient();
            await _gameClient!.PlayAttackCardAsync(accessToken, cardId, cancellationToken);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    public async Task BeatAttackCardAsync(
        string accessToken,
        string attackerCardId,
        string defenderCardId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            EnsureGameClient();
            await _gameClient!.BeatAttackCardAsync(accessToken, attackerCardId, defenderCardId, cancellationToken);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    public async Task GiveUpDefenceAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            EnsureGameClient();
            await _gameClient!.GiveUpDefenceAsync(accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    public void Disconnect()
    {
        _streamCts?.Cancel();
        _accessToken = null;
        Status = ConnectionStatus.Idle;
        NotifyStateChanged();
    }

    private async Task ListenForEventsAsync(string gameId, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var response in _gameClient!.GetGameEventsAsync(gameId, cancellationToken))
            {
                if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.OldEvent)
                {
                    ApplyEvent(response.OldEvent.Event);
                }
                else if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.NewEvent)
                {
                    ApplyEvent(response.NewEvent.Event);
                }
                else if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.OldEventsEnded)
                {
                    NotifyStateChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    private void ApplyEvent(Any payload)
    {
        GameStateReducer.Apply(_state, payload);
        NotifyStateChanged();
    }

    private Uri BuildGameServiceEndpoint(string playerIdToken)
    {
        var baseUri = new Uri(_settings.Services.GameServiceUrl);
        var builder = new UriBuilder(baseUri);
        var queryPrefix = string.IsNullOrWhiteSpace(builder.Query) ? string.Empty : builder.Query.TrimStart('?') + "&";
        builder.Query = $"{queryPrefix}player_id_token={Uri.EscapeDataString(playerIdToken)}";
        return builder.Uri;
    }

    private void EnsureGameClient()
    {
        if (_gameClient is null)
        {
            throw new InvalidOperationException("GameServiceClient is not initialized");
        }
    }

    private void ResetError()
    {
        ErrorMessage = null;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
