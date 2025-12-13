namespace GameService.Domain;

public record Round(
    int RoundNumber,
    string FirstAttackerId,
    string FirstDefenderId,
    string CurrentAttackerId,
    string CurrentDefenderId,
    string? ActivePlayerId,
    bool DefenceGivenUp
) { }
