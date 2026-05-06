using Acorn.Frame;
using NUnit.Framework;

namespace Acorn.Tests;

[TestFixture]
public class ByteBufferWriterTests
{
    [Test]
    public void ByteBufferWriter_Creation_WithCapacity()
    {
        var writer = new ByteBufferWriter(128);
        Assert.That(writer.Position, Is.EqualTo(0));
        Assert.That(writer.Length, Is.GreaterThanOrEqualTo(128));
        Assert.That(writer.Remaining, Is.GreaterThanOrEqualTo(128));
    }

    [Test]
    public void ByteBufferWriter_Creation_WithSpan()
    {
        var buffer = new byte[64];
        var writer = new ByteBufferWriter(buffer.AsSpan());
        Assert.That(writer.Position, Is.EqualTo(0));
        Assert.That(writer.Length, Is.EqualTo(64));
    }

    [Test]
    public void ByteBufferWriter_WriteI32LE_Advances()
    {
        var writer = new ByteBufferWriter(128);
        var posBefore = writer.Position;
        writer.WriteI32LE(42);
        Assert.That(writer.Position, Is.EqualTo(posBefore + 4));
    }

    [Test]
    public void ByteBufferWriter_WriteU32LE_Advances()
    {
        var writer = new ByteBufferWriter(128);
        var posBefore = writer.Position;
        writer.WriteU32LE(42u);
        Assert.That(writer.Position, Is.EqualTo(posBefore + 4));
    }

    [Test]
    public void ByteBufferWriter_WriteF32LE_Advances()
    {
        var writer = new ByteBufferWriter(128);
        var posBefore = writer.Position;
        writer.WriteF32LE(3.14f);
        Assert.That(writer.Position, Is.EqualTo(posBefore + 4));
    }

    [Test]
    public void ByteBufferWriter_WriteF64LE_Advances()
    {
        var writer = new ByteBufferWriter(128);
        var posBefore = writer.Position;
        writer.WriteF64LE(3.14159265358979);
        Assert.That(writer.Position, Is.EqualTo(posBefore + 8));
    }

    [Test]
    public void ByteBufferWriter_Write_WritesData()
    {
        var writer = new ByteBufferWriter(128);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        writer.Write(data);
        Assert.That(writer.Position, Is.EqualTo(5));
    }

    [Test]
    public void ByteBufferWriter_WriteStringUTF8_Writes()
    {
        var writer = new ByteBufferWriter(1024);
        writer.WriteString("Hello, Acorn!");
        Assert.That(writer.Position, Is.GreaterThan(0));
    }

    [Test]
    public void ByteBufferWriter_WrittenData_ReflectsWritten()
    {
        var writer = new ByteBufferWriter(128);
        writer.WriteI32LE(42);
        writer.WriteI32LE(99);
        var written = writer.WrittenData;
        Assert.That(written.Length, Is.GreaterThanOrEqualTo(8));
    }
}
