namespace BotService.Domain;

public record Round(
    string FirstAttackerId,
    string FirstDefenderId,
    string CurrentAttackerId,
    string CurrentDefenderId,
    string ActivePlayerId
);
