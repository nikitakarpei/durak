using GameService.Api.Data;

namespace BlazorPWA.State;

public sealed class GameState
{
    public string GameId { get; set; } = string.Empty;
    public int? TotalPlayerSeats { get; set; }
    public string? LocalPlayerId { get; set; }
    public Dictionary<string, PlayerState> Players { get; } = new();
    public Dictionary<string, Card> CardsById { get; } = new();
    public List<TablePair> Table { get; } = new();
    public List<Card> YourHand { get; } = new();
    public string? AttackerPlayerId { get; set; }
    public string? DefenderPlayerId { get; set; }
    public string? CurrentTurnPlayerId { get; set; }
    public RegularCard? TrumpCard { get; set; }
    public int TalonCount { get; set; }
    public List<PossibleMove> YourPossibleMoves { get; } = new();
    public TimeSpan? PlayerAfkTimerDuration { get; set; }
    public int TurnSequence { get; set; }
}

public sealed record PlayerState(string PlayerId, int Seat)
{
    public int CardCount { get; set; }
    public bool IsFool { get; set; }
}

public sealed record Card(string Id, CardSuit Suit, CardRank Rank);

public sealed record TablePair(Card Attack, Card? Defence);

public sealed record PossibleMove(PossibleMoveType Type, string? YourCardId, string? TargetCardId);

public enum PossibleMoveType
{
    PlayAttackCard,
    BeatAttackCard,
    GiveUpDefence
}
