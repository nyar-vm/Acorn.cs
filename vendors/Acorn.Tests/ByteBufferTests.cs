using Acorn.Frame;
using NUnit.Framework;

namespace Acorn.Tests;

[TestFixture]
public class ByteBufferTests
{
    [Test]
    public void ByteBuffer_Creation_WithData()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var buffer = new ByteBuffer(data.AsSpan());
        Assert.That(buffer.Position, Is.EqualTo(0));
        Assert.That(buffer.Length, Is.EqualTo(4));
        Assert.That(buffer.Remaining, Is.EqualTo(4));
        Assert.That(buffer.IsEnd, Is.False);
    }

    [Test]
    public void ByteBuffer_IsEnd_WhenPositionAtEnd()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var buffer = new ByteBuffer(data.AsSpan());
        buffer.Position = 4;
        Assert.That(buffer.IsEnd, Is.True);
        Assert.That(buffer.Remaining, Is.EqualTo(0));
    }

    [Test]
    public void ByteBuffer_ReadU32_ReturnsValue()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteU32LE(123456u);
        var buffer = new ByteBuffer(writer.WrittenData);
        var val = buffer.ReadU32();
        Assert.That(val, Is.EqualTo(123456u));
    }

    [Test]
    public void ByteBuffer_ReadI32_ReturnsValue()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteI32LE(-12345);
        var buffer = new ByteBuffer(writer.WrittenData);
        var val = buffer.ReadI32();
        Assert.That(val, Is.EqualTo(-12345));
    }

    [Test]
    public void ByteBuffer_ReadF32_ReturnsValue()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteF32LE(3.14f);
        var buffer = new ByteBuffer(writer.WrittenData);
        var val = buffer.ReadF32();
        Assert.That(val, Is.EqualTo(3.14f));
    }

    [Test]
    public void ByteBuffer_ReadF64_ReturnsValue()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteF64LE(2.718281828);
        var buffer = new ByteBuffer(writer.WrittenData);
        var val = buffer.ReadF64();
        Assert.That(val, Is.EqualTo(2.718281828));
    }

    [Test]
    public void ByteBuffer_Skip_Correctly()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteI32LE(1);
        writer.WriteI32LE(2);
        writer.WriteI32LE(3);
        var buffer = new ByteBuffer(writer.WrittenData);

        var first = buffer.ReadI32();
        buffer.Position += 4;
        var third = buffer.ReadI32();

        Assert.That(first, Is.EqualTo(1));
        Assert.That(third, Is.EqualTo(3));
    }
}
