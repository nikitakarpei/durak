namespace BotService.Domain;

public record JokerCard(string Id, CardLocation Location, JokerColor Color) : Card(Id, Location);
