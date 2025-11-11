namespace GameService.Domain;

public interface IRandomNumberGenerator
{
    public (int Value, byte[] NewSeed) GetInt32(byte[] seed, int maxValue);
}
