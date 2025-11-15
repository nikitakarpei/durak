using System.Buffers.Binary;
using System.Security.Cryptography;

namespace GameService.Infrastructure;

public interface IRandomNumberGenerator
{
    public (int Value, byte[] NewSeed) GetInt32(byte[] seed, int maxValue);
}

public class RandomNumberGenerator : IRandomNumberGenerator
{
    public (int Value, byte[] NewSeed) GetInt32(byte[] seed, int maxValue)
    {
        if (seed == null || seed.Length == 0)
        {
            throw new ArgumentException("Seed cannot be null or empty", nameof(seed));
        }

        if (maxValue <= 0)
        {
            throw new ArgumentException("maxValue must be greater than 0", nameof(maxValue));
        }

        // Derive next seed
        byte[] newSeed = HMACSHA256.HashData(seed, "next"u8.ToArray());

        uint bound = (uint)maxValue;
        const ulong space = 1UL << 32;
        ulong threshold = space - (space % bound);

        ulong counter = 0;
        Span<byte> ctr = stackalloc byte[8];
        Span<byte> block = stackalloc byte[32];

        while (true)
        {
            BinaryPrimitives.WriteUInt64BigEndian(ctr, counter++);
            HMACSHA256.HashData(newSeed, ctr, block);

            for (int i = 0; i < 8; i++)
            {
                uint r = BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4));
                if (r < threshold)
                {
                    int value = (int)(r % bound);
                    return (value, newSeed);
                }
            }
        }
    }
}
