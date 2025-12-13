namespace GameService.Domain;

public interface IShuffledPositionsGenerator
{
    public (int[] Positions, byte[] NewSeed) GenerateShuffledPositions(byte[] seed, int count);
}
