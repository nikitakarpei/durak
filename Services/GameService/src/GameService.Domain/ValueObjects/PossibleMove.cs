using System.Text.Json.Serialization;

namespace GameService.Domain;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(PossibleMoves.PlayAttackCard), typeDiscriminator: "playAttackCard")]
[JsonDerivedType(typeof(PossibleMoves.BeatAttackCard), typeDiscriminator: "beatAttackCard")]
[JsonDerivedType(typeof(PossibleMoves.GiveUpDefence), typeDiscriminator: "giveUpDefence")]
public abstract record PossibleMove();
