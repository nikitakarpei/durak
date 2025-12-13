using System.Text.Json.Serialization;

namespace GameService.Domain;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(CardLocations.Hand), typeDiscriminator: "hand")]
[JsonDerivedType(typeof(CardLocations.Talon), typeDiscriminator: "talon")]
[JsonDerivedType(typeof(CardLocations.DiscardPile), typeDiscriminator: "discardPile")]
[JsonDerivedType(typeof(CardLocations.Table), typeDiscriminator: "table")]
public abstract record CardLocation;
