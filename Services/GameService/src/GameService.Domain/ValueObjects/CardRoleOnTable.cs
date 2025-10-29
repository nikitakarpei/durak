using System.Text.Json.Serialization;

namespace GameService.Domain;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(CardRolesOnTable.Attack), typeDiscriminator: "attack")]
[JsonDerivedType(typeof(CardRolesOnTable.Defence), typeDiscriminator: "defence")]
public abstract record CardRoleOnTable;
