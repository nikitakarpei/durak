namespace GameService.Domain;

public record Player(string Id, int Seat, bool Finished)
{
    public virtual bool Equals(Player? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
