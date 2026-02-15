using Rpc.Client;

namespace BlazorPWA.Services.Api;

public sealed class GameServiceClient
{
    private const string GetGameAccessTokenMethod = "GetGameAccessToken";
    private const string JoinLobbyMethod = "JoinLobby";
    private const string PlayAttackCardMethod = "PlayAttackCard";
    private const string BeatAttackCardMethod = "BeatAttackCard";
    private const string GiveUpDefenceMethod = "GiveUpDefence";
    private const string GetGameEventsMethod = "GetGameEvents";

    private readonly IClient _client;

    public GameServiceClient(IClient client)
    {
        _client = client;
    }

    public async Task<string> GetGameAccessTokenAsync(string gameId, CancellationToken cancellationToken)
    {
        var request = new GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request
        {
            GameId = gameId,
        };

        var response = await _client.UnaryCall<
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request,
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response
        >(GetGameAccessTokenMethod, request, cancellationToken);

        return response.Success.AccessToken;
    }

    public async Task JoinLobbyAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new GameService.Api.Commands.JoinLobbyCommand.Types.Request
        {
            AccessToken = accessToken,
        };

        _ = await _client.UnaryCall<
            GameService.Api.Commands.JoinLobbyCommand.Types.Request,
            GameService.Api.Commands.JoinLobbyCommand.Types.Response
        >(JoinLobbyMethod, request, cancellationToken);
    }

    public async Task PlayAttackCardAsync(string accessToken, string cardId, CancellationToken cancellationToken)
    {
        var request = new GameService.Api.Commands.PlayAttackCardCommand.Types.Request
        {
            AccessToken = accessToken,
            CardId = cardId,
        };

        _ = await _client.UnaryCall<
            GameService.Api.Commands.PlayAttackCardCommand.Types.Request,
            GameService.Api.Commands.PlayAttackCardCommand.Types.Response
        >(PlayAttackCardMethod, request, cancellationToken);
    }

    public async Task BeatAttackCardAsync(
        string accessToken,
        string attackerCardId,
        string defenderCardId,
        CancellationToken cancellationToken
    )
    {
        var request = new GameService.Api.Commands.BeatAttackCardCommand.Types.Request
        {
            AccessToken = accessToken,
            AttackerCardId = attackerCardId,
            DefenderCardId = defenderCardId,
        };

        _ = await _client.UnaryCall<
            GameService.Api.Commands.BeatAttackCardCommand.Types.Request,
            GameService.Api.Commands.BeatAttackCardCommand.Types.Response
        >(BeatAttackCardMethod, request, cancellationToken);
    }

    public async Task GiveUpDefenceAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new GameService.Api.Commands.GiveUpDefenceCommand.Types.Request
        {
            AccessToken = accessToken,
        };

        _ = await _client.UnaryCall<
            GameService.Api.Commands.GiveUpDefenceCommand.Types.Request,
            GameService.Api.Commands.GiveUpDefenceCommand.Types.Response
        >(GiveUpDefenceMethod, request, cancellationToken);
    }

    public IAsyncEnumerable<GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse> GetGameEventsAsync(
        string gameId,
        CancellationToken cancellationToken
    )
    {
        var request = new GameService.Api.Commands.GetGameEventsCommand.Types.Request
        {
            GameId = gameId,
        };

        return _client.StreamingCall<
            GameService.Api.Commands.GetGameEventsCommand.Types.Request,
            GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse
        >(GetGameEventsMethod, request, cancellationToken);
    }
}
