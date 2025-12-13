namespace GameService.Infrastructure;

public class ShuffledPositionsGenerator(IRandomNumberGenerator randomNumberGenerator)
    : Domain.IShuffledPositionsGenerator
{
    public (int[] Positions, byte[] NewSeed) GenerateShuffledPositions(byte[] seed, int count)
    {
        var positions = Enumerable.Range(0, count).ToArray();
        for (var i = positions.Length - 1; i >= 1; i--)
        {
            var (j, newSeed) = randomNumberGenerator.GetInt32(seed, i + 1);
            seed = newSeed;
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        return (positions, seed);
    }
}
