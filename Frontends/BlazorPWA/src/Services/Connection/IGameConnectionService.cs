namespace BlazorPWA.Services.Connection;

public interface IGameConnectionService
{
    ConnectionStatus Status { get; }
    bool IsReplayingHistory { get; }
    string? ErrorMessage { get; }
    string? AccessToken { get; }
    event Action? StateChanged;
    Task ConnectAsync(string gameId, string customId, CancellationToken cancellationToken);
    Task PlayAttackCardAsync(string accessToken, string cardId, CancellationToken cancellationToken);
    Task BeatAttackCardAsync(string accessToken, string attackerCardId, string defenderCardId, CancellationToken cancellationToken);
    Task GiveUpDefenceAsync(string accessToken, CancellationToken cancellationToken);
    void Disconnect();
}
