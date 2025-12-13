namespace BotService.Domain;

public record UnrevealedCard(string Id, CardLocation Location) : Card(Id, Location);
