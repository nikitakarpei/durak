using System.Text.Json.Serialization;

namespace GameService.Domain;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(GameOptions.AmountOfPlayers), typeDiscriminator: "amountOfPlayers")]
[JsonDerivedType(typeof(GameOptions.CardRanksInTalon), typeDiscriminator: "cardRanksInTalon")]
[JsonDerivedType(typeof(GameOptions.MinHandSize), typeDiscriminator: "minHandSize")]
public abstract record GameOption;
