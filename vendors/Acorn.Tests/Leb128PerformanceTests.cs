using System.Diagnostics;
using Acorn.Frame;
using NUnit.Framework;

namespace Acorn.Tests;

/// <summary>
/// M5 LEB128 编解码正确性与性能基准测试
/// </summary>
[TestFixture]
public class Leb128PerformanceTests
{
    #region 正确性测试

    /// <summary>
    /// U32 零值编码往返
    /// </summary>
    [Test]
    public void Leb128_U32_Zero_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(0u);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(0u));
        Assert.That(consumed, Is.EqualTo(1));
    }

    /// <summary>
    /// U32 小值单字节编码
    /// </summary>
    [Test]
    public void Leb128_U32_SmallValue_SingleByte()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(127u);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(127u));
        Assert.That(consumed, Is.EqualTo(1));
    }

    /// <summary>
    /// U32 大值多字节编码
    /// </summary>
    [Test]
    public void Leb128_U32_LargeValue_MultipleBytes()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(300u);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(300u));
        Assert.That(consumed, Is.GreaterThan(1));
    }

    /// <summary>
    /// U32 最大值编码往返
    /// </summary>
    [Test]
    public void Leb128_U32_MaxValue_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(uint.MaxValue);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(uint.MaxValue));
        Assert.That(consumed, Is.LessThanOrEqualTo(5));
    }

    /// <summary>
    /// U64 零值编码往返
    /// </summary>
    [Test]
    public void Leb128_U64_Zero_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U64(0UL);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U64(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(0UL));
        Assert.That(consumed, Is.EqualTo(1));
    }

    /// <summary>
    /// U64 最大值编码往返
    /// </summary>
    [Test]
    public void Leb128_U64_MaxValue_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U64(ulong.MaxValue);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U64(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(ulong.MaxValue));
        Assert.That(consumed, Is.LessThanOrEqualTo(10));
    }

    /// <summary>
    /// I32 正值编码往返
    /// </summary>
    [Test]
    public void Leb128_I32_Positive_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I32(42);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128I32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(42));
        Assert.That(consumed, Is.GreaterThan(0));
    }

    /// <summary>
    /// I32 负值编码往返
    /// </summary>
    [Test]
    public void Leb128_I32_Negative_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I32(-1);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128I32(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(-1));
        Assert.That(consumed, Is.GreaterThan(0));
    }

    /// <summary>
    /// I64 正值编码往返
    /// </summary>
    [Test]
    public void Leb128_I64_Positive_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I64(123456789L);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128I64(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(123456789L));
        Assert.That(consumed, Is.GreaterThan(0));
    }

    /// <summary>
    /// I64 负值编码往返
    /// </summary>
    [Test]
    public void Leb128_I64_Negative_RoundTrip()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I64(-987654321);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128I64(data, out var value, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(-987654321));
        Assert.That(consumed, Is.GreaterThan(0));
    }

    #endregion

    #region 边界条件测试

    /// <summary>
    /// 空数据解码应失败
    /// </summary>
    [Test]
    public void Leb128_Decode_EmptyData_ShouldFail()
    {
        var empty = ReadOnlySpan<byte>.Empty;
        var ok = ByteBuffer.TryDecodeLeb128U32(empty, out _, out _);
        Assert.That(ok, Is.False);
    }

    /// <summary>
    /// 截断数据解码应失败
    /// </summary>
    [Test]
    public void Leb128_Decode_TruncatedData_ShouldFail()
    {
        var truncated = new byte[] { 0x80 };
        var ok = ByteBuffer.TryDecodeLeb128U32(truncated, out _, out _);
        Assert.That(ok, Is.False);
    }

    /// <summary>
    /// 超大 U32 数据（超过 5 字节）应失败
    /// </summary>
    [Test]
    public void Leb128_Decode_U32_TooLong_ShouldFail()
    {
        var tooLong = new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 };
        var ok = ByteBuffer.TryDecodeLeb128U32(tooLong, out _, out _);
        Assert.That(ok, Is.False);
    }

    /// <summary>
    /// 超大 U64 数据（超过 10 字节）应失败
    /// </summary>
    [Test]
    public void Leb128_Decode_U64_TooLong_ShouldFail()
    {
        var tooLong = new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02 };
        var ok = ByteBuffer.TryDecodeLeb128U64(tooLong, out _, out _);
        Assert.That(ok, Is.False);
    }

    /// <summary>
    /// 各种 U32 值的往返测试
    /// </summary>
    [Test]
    public void Leb128_U32_RoundTrip_MultipleValues(
        [Values(0u, 1u, 127u, 128u, 255u, 16383u, 16384u, 2097151u, 2097152u, 268435455u, uint.MaxValue)]
        uint expected)
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(expected);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var value, out _);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    /// <summary>
    /// 各种 U64 值的往返测试
    /// </summary>
    [Test]
    public void Leb128_U64_RoundTrip_MultipleValues(
        [Values(0UL, 127UL, 16383UL, 2097151UL, 268435455UL, 34359738367UL, 4398046511103UL, ulong.MaxValue)]
        ulong expected)
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U64(expected);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U64(data, out var value, out _);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    /// <summary>
    /// 各种 I32 值的往返测试
    /// </summary>
    [Test]
    public void Leb128_I32_RoundTrip_MultipleValues(
        [Values(0, 1, -1, 127, 128, -128, -129, 16383, -16383, 16384, -16384, int.MaxValue, int.MinValue)]
        int expected)
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I32(expected);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128I32(data, out var value, out _);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    #endregion

    #region String LEB128

    /// <summary>
    /// LEB128 字符串编码往返
    /// </summary>
    [Test]
    public void Leb128_String_RoundTrip()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteLeb128String("hello leb128");
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var length, out var consumed);
        Assert.That(ok, Is.True);
        Assert.That(length, Is.EqualTo(12));

        var str = System.Text.Encoding.UTF8.GetString(data.Slice(consumed, (int)length));
        Assert.That(str, Is.EqualTo("hello leb128"));
    }

    /// <summary>
    /// 空字符串 LEB128 编码
    /// </summary>
    [Test]
    public void Leb128_EmptyString_EncodesZero()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteLeb128String(string.Empty);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var length, out _);
        Assert.That(ok, Is.True);
        Assert.That(length, Is.EqualTo(0));
    }

    /// <summary>
    /// 空字符串编码为零长度
    /// </summary>
    [Test]
    public void Leb128_NullString_EncodesZero()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteLeb128String(string.Empty);
        var data = writer.WrittenData;

        var ok = ByteBuffer.TryDecodeLeb128U32(data, out var length, out _);
        Assert.That(ok, Is.True);
        Assert.That(length, Is.EqualTo(0));
    }

    #endregion

    #region 性能基准测试

    /// <summary>
    /// U32 编码 100000 次性能
    /// </summary>
    [Test]
    public void Leb128_U32_Encode_100000_PerformanceBaseline()
    {
        var writer = new ByteBufferWriter(500000);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100000; i++)
        {
            writer.WriteLeb128U32(999999u);
        }

        sw.Stop();
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100), $"U32 编码 100000 次耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// U32 解码 100000 次性能
    /// </summary>
    [Test]
    public void Leb128_U32_Decode_100000_PerformanceBaseline()
    {
        var writer = new ByteBufferWriter(500000);
        for (var i = 0; i < 100000; i++)
        {
            writer.WriteLeb128U32(999999u);
        }

        var data = writer.WrittenData;
        var sw = Stopwatch.StartNew();

        var pos = 0;
        for (var i = 0; i < 100000; i++)
        {
            ByteBuffer.TryDecodeLeb128U32(data.Slice(pos), out _, out var consumed);
            pos += consumed;
        }

        sw.Stop();
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100), $"U32 解码 100000 次耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// U64 编码 100000 次性能
    /// </summary>
    [Test]
    public void Leb128_U64_Encode_100000_PerformanceBaseline()
    {
        var writer = new ByteBufferWriter(1000000);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100000; i++)
        {
            writer.WriteLeb128U64(999999999999UL);
        }

        sw.Stop();
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(150), $"U64 编码 100000 次耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// U64 解码 100000 次性能
    /// </summary>
    [Test]
    public void Leb128_U64_Decode_100000_PerformanceBaseline()
    {
        var writer = new ByteBufferWriter(1000000);
        for (var i = 0; i < 100000; i++)
        {
            writer.WriteLeb128U64(999999999999UL);
        }

        var data = writer.WrittenData;
        var sw = Stopwatch.StartNew();

        var pos = 0;
        for (var i = 0; i < 100000; i++)
        {
            ByteBuffer.TryDecodeLeb128U64(data.Slice(pos), out _, out var consumed);
            pos += consumed;
        }

        sw.Stop();
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(150), $"U64 解码 100000 次耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 混合大小值编码吞吐量
    /// </summary>
    [Test]
    public void Leb128_Mixed_Sizes_Throughput()
    {
        var writer = new ByteBufferWriter(500000);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 50000; i++)
        {
            writer.WriteLeb128U32((uint)(i % 10));       // 单字节
            writer.WriteLeb128U32((uint)(i * 100));       // 2 字节
            writer.WriteLeb128U32((uint)(i * 1000000));   // 多字节
        }

        sw.Stop();
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100), $"混合编码 150000 次耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// ByteBufferWriter 多次扩容不应丢失数据
    /// </summary>
    [Test]
    public void ByteBufferWriter_Resize_ShouldNotLoseData()
    {
        var writer = new ByteBufferWriter(16); // 小初始缓冲区
        var expected = new uint[200];
        for (var i = 0; i < 200; i++)
        {
            expected[i] = (uint)(i * 13 + 7); // 非连续值避免缓存模式
            writer.WriteLeb128U32(expected[i]);
        }

        var data = writer.WrittenData;
        var pos = 0;
        for (var i = 0; i < 200; i++)
        {
            var ok = ByteBuffer.TryDecodeLeb128U32(data.Slice(pos), out var value, out var consumed);
            Assert.That(ok, Is.True, $"解码第 {i} 项失败");
            Assert.That(value, Is.EqualTo(expected[i]), $"第 {i} 项值不匹配");
            pos += consumed;
        }
    }

    #endregion
}
