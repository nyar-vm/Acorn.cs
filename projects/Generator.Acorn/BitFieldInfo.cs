namespace Generator.Acorn;

internal sealed class BitFieldInfo
{
    public int BitOffset { get; }
    public int BitCount { get; }

    public BitFieldInfo(int bitOffset, int bitCount)
    {
        BitOffset = bitOffset;
        BitCount = bitCount;
    }
}