using BlazorPWA.Configuration;
using BlazorPWA.Services.Api;
using BlazorPWA.State;
using GameService.Api.Commands;
using GameService.Api.GameEvents;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace BlazorPWA.Services.Connection;

public sealed class GameConnectionService : IGameConnectionService
{
    private readonly Func<Uri, IGameServiceClient> _gameClientFactory;
    private readonly Func<Uri, IPlayerIdentityServiceClient> _identityClientFactory;
    private readonly AppSettings _settings;
    private readonly GameState _state;

    private IGameServiceClient? _gameClient;
    private CancellationTokenSource? _streamCts;
    private string? _accessToken;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Idle;
    public bool IsReplayingHistory { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? AccessToken => _accessToken;

    public event Action? StateChanged;

    public GameConnectionService(
        Func<Uri, IGameServiceClient> gameClientFactory,
        Func<Uri, IPlayerIdentityServiceClient> identityClientFactory,
        IOptions<AppSettings> options,
        GameState state)
    {
        _gameClientFactory = gameClientFactory;
        _identityClientFactory = identityClientFactory;
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
            var identityClient = _identityClientFactory(identityEndpoint);

            var effectiveCustomId = string.IsNullOrWhiteSpace(customId) ? _settings.Auth.CustomId : customId;
            var loginResult = await identityClient.LoginWithCustomIdAsync(effectiveCustomId, cancellationToken);
            _state.LocalPlayerId = loginResult.PlayerId;

            var gameEndpoint = BuildGameServiceEndpoint(loginResult.PlayerIdToken);
            _gameClient = _gameClientFactory(gameEndpoint);

            var accessToken = await _gameClient.GetGameAccessTokenAsync(gameId, cancellationToken);
            _accessToken = accessToken;

            _streamCts?.Cancel();
            _streamCts = new CancellationTokenSource();

            var oldEventsEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = Task.Run(
                () => ListenForEventsAsync(gameId, oldEventsEnded, _streamCts.Token),
                _streamCts.Token);

            await oldEventsEnded.Task.WaitAsync(cancellationToken);

            if (!_state.Players.ContainsKey(_state.LocalPlayerId!))
            {
                await _gameClient.JoinLobbyAsync(accessToken, cancellationToken);
            }

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

    private async Task ListenForEventsAsync(
        string gameId,
        TaskCompletionSource oldEventsEnded,
        CancellationToken cancellationToken)
    {
        IsReplayingHistory = true;
        NotifyStateChanged();

        try
        {
            await foreach (var response in _gameClient!.GetGameEventsAsync(gameId, cancellationToken))
            {
                if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.OldEvent)
                {
                    await ApplyEventAsync(response.OldEvent.Event, cancellationToken);
                }
                else if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.NewEvent)
                {
                    await ApplyEventAsync(response.NewEvent.Event, cancellationToken);
                }
                else if (response.ResultCase == GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.OldEventsEnded)
                {
                    IsReplayingHistory = false;
                    NotifyStateChanged();
                    oldEventsEnded.TrySetResult();
                }
            }
        }
        catch (Exception ex)
        {
            IsReplayingHistory = false;
            oldEventsEnded.TrySetException(ex);
            Status = ConnectionStatus.Error;
            ErrorMessage = ex.Message;
            NotifyStateChanged();
        }
    }

    // TECH DEBT: 1s artificial delay before RoundFinished to let players see the final table state.
    // Replace with a proper server-driven acknowledgement flow.
    private async Task ApplyEventAsync(Any payload, CancellationToken cancellationToken)
    {
        if (payload.Is(RoundFinished.Descriptor))
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

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
