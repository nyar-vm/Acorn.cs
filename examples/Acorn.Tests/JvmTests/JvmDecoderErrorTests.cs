using Acorn.Jvm.Data;
using Acorn.Jvm.Decode;

namespace Acorn.Tests.JvmTests;

/// <summary>
///     JVM ClassFile 解码器异常路径测试，验证解码器在异常输入下的行为。
/// </summary>
public class JvmDecoderErrorTests
{
    private readonly JvmDecoder _decoder = new();

    #region 魔数验证

    [Fact]
    public void Decode_InvalidMagic_ThrowsInvalidDataException()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        var ex = Assert.Throws<InvalidDataException>(() => _decoder.Decode(data));
        Assert.Contains("魔数", ex.Message);
    }

    [Fact]
    public void Decode_EmptyData_ThrowsException()
    {
        var data = Array.Empty<byte>();

        Assert.ThrowsAny<Exception>(() => _decoder.Decode(data));
    }

    [Fact]
    public void Decode_PartialMagic_ThrowsException()
    {
        var data = new byte[] { 0xCA, 0xFE };

        Assert.ThrowsAny<Exception>(() => _decoder.Decode(data));
    }

    #endregion

    #region 截断数据

    [Fact]
    public void Decode_TruncatedAfterMagic_ThrowsException()
    {
        var data = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };

        Assert.ThrowsAny<Exception>(() => _decoder.Decode(data));
    }

    [Fact]
    public void Decode_TruncatedConstantPool_ThrowsException()
    {
        var data = new byte[]
        {
            0xCA, 0xFE, 0xBA, 0xBE,
            0x00, 0x00,
            0x00, 0x41,
            0x00, 0x10
        };

        Assert.ThrowsAny<Exception>(() => _decoder.Decode(data));
    }

    #endregion

    #region 常量池损坏

    [Fact]
    public void Decode_InvalidConstantPoolTag_ThrowsException()
    {
        var data = new byte[]
        {
            0xCA, 0xFE, 0xBA, 0xBE,
            0x00, 0x00,
            0x00, 0x41,
            0x00, 0x02,
            0xFF,
            0x00
        };

        Assert.ThrowsAny<Exception>(() => _decoder.Decode(data));
    }

    #endregion
}
