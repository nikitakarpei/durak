using System.Text.Json.Serialization;

namespace GameService.Domain;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(JokerCard), typeDiscriminator: "joker")]
[JsonDerivedType(typeof(RegularCard), typeDiscriminator: "regular")]
public abstract record Card(string Id, CardLocation Location)
{
    public virtual bool Equals(Card? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
