namespace GameService.Api;

internal static class PossibleMoveExtensions
{
    public static Google.Protobuf.WellKnownTypes.Any ToApiDto(this Domain.PossibleMove possibleMove)
    {
        return possibleMove switch
        {
            Domain.PossibleMoves.PlayAttackCard playAttackCard => Google.Protobuf.WellKnownTypes.Any.Pack(
                new PossibleMoves.PlayAttackCard { YourCardId = playAttackCard.YourCardId }
            ),
            Domain.PossibleMoves.BeatAttackCard beatAttackCard => Google.Protobuf.WellKnownTypes.Any.Pack(
                new PossibleMoves.BeatAttackCard
                {
                    YourCardId = beatAttackCard.YourCardId,
                    TargetCardId = beatAttackCard.TargetCardId,
                }
            ),
            Domain.PossibleMoves.GiveUpDefence => Google.Protobuf.WellKnownTypes.Any.Pack(
                new PossibleMoves.GiveUpDefence()
            ),
            _ => throw new NotImplementedException(),
        };
    }
}
