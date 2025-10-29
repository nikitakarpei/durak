namespace BotService.Domain;

public record RegularCard(string Id, CardLocation Location, CardSuit Suit, CardRank Rank) : Card(Id, Location);
