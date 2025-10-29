namespace BotService.Domain;

public static class CardLocations
{
    public record DiscardPile() : CardLocation;

    public record Hand(string PlayerId) : CardLocation;

    public record Table(DateTime PlayedAt, CardRoleOnTable Role) : CardLocation;

    public record Talon(int Position) : CardLocation;
}
