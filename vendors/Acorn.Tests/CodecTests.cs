using Acorn.Codec;
using NUnit.Framework;

namespace Acorn.Tests;

[TestFixture]
public class ZigZagTests
{
    [Test]
    public void ZigZag_Int_ZeroRoundTrips()
    {
        var encoded = ZigZag.Encode(0);
        var decoded = ZigZag.Decode(encoded);
        Assert.That(decoded, Is.EqualTo(0));
    }

    [Test]
    public void ZigZag_Int_NegativeOne()
    {
        var encoded = ZigZag.Encode(-1);
        Assert.That(encoded, Is.EqualTo(1u));
        var decoded = ZigZag.Decode(encoded);
        Assert.That(decoded, Is.EqualTo(-1));
    }

    [Test]
    public void ZigZag_Int_PositiveOne()
    {
        var encoded = ZigZag.Encode(1);
        Assert.That(encoded, Is.EqualTo(2u));
        var decoded = ZigZag.Decode(encoded);
        Assert.That(decoded, Is.EqualTo(1));
    }

    [Test]
    public void ZigZag_Long_ZeroRoundTrips()
    {
        var encoded = ZigZag.Encode(0L);
        var decoded = ZigZag.Decode(encoded);
        Assert.That(decoded, Is.EqualTo(0L));
    }

    [Test]
    public void ZigZag_Long_RoundTrips()
    {
        var values = new long[] { long.MinValue, long.MaxValue, -1, 0, 1, 42, -42, 1234567890123L };
        foreach (var val in values)
        {
            var encoded = ZigZag.Encode(val);
            var decoded = ZigZag.Decode(encoded);
            Assert.That(decoded, Is.EqualTo(val), $"值 {val} 往返失败");
        }
    }

    [Test]
    public void ZigZag_Short_RoundTrips()
    {
        var values = new short[] { short.MinValue, short.MaxValue, -1, 0, 1, 42, -42 };
        foreach (var val in values)
        {
            var encoded = ZigZag.Encode(val);
            var decoded = ZigZag.Decode(encoded);
            Assert.That(decoded, Is.EqualTo(val), $"值 {val} 往返失败");
        }
    }

    [Test]
    public void ZigZag_Int_RoundTrips()
    {
        var values = new int[] { int.MinValue, int.MaxValue, -1, 0, 1, 42, -42, 1234567 };
        foreach (var val in values)
        {
            var encoded = ZigZag.Encode(val);
            var decoded = ZigZag.Decode(encoded);
            Assert.That(decoded, Is.EqualTo(val), $"值 {val} 往返失败");
        }
    }

    [Test]
    public void ZigZag_SmallValues_AbsoluteValueOrder()
    {
        Assert.That(ZigZag.Encode(0), Is.LessThan(ZigZag.Encode(1)));
        Assert.That(ZigZag.Encode(1), Is.LessThan(ZigZag.Encode(2)));
        Assert.That(ZigZag.Encode(-1), Is.LessThan(ZigZag.Encode(-2)));
        Assert.That(ZigZag.Encode(-1), Is.LessThan(ZigZag.Encode(2)));
    }
}

[TestFixture]
public class EndiannessTests
{
    [Test]
    public void Endianness_Enum_HasBothValues()
    {
        var values = Enum.GetValues<Endianness>();
        Assert.That(values.Length, Is.EqualTo(2));
        Assert.That(values, Does.Contain(Endianness.LittleEndian));
        Assert.That(values, Does.Contain(Endianness.BigEndian));
    }
}
