using GameService.Api.Commands;

namespace BlazorPWA.Services.Api;

public interface IGameServiceClient
{
    Task<string> GetGameAccessTokenAsync(string gameId, CancellationToken cancellationToken);
    Task JoinLobbyAsync(string accessToken, CancellationToken cancellationToken);
    Task PlayAttackCardAsync(string accessToken, string cardId, CancellationToken cancellationToken);
    Task BeatAttackCardAsync(string accessToken, string attackerCardId, string defenderCardId, CancellationToken cancellationToken);
    Task GiveUpDefenceAsync(string accessToken, CancellationToken cancellationToken);
    IAsyncEnumerable<GetGameEventsCommand.Types.StreamResponse> GetGameEventsAsync(string gameId, CancellationToken cancellationToken);
}
