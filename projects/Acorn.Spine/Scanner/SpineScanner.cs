using System.Text;
using Acorn.Frame;

namespace Acorn.Spine.Scanner;

/// <summary>
///     Spine 格式扫描器的默认实现，基于 <see cref="SpanScanner" /> 提供零分配的快速数据扫描。
/// </summary>
public ref struct SpineScanner : ISpineScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="SpineScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 Spine 二进制数据。</param>
    public SpineScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <inheritdoc />
    public string ReadHash()
    {
        var hashLength = _scanner.Buffer.ReadU8();

        if (hashLength == 0)
        {
            return string.Empty;
        }

        return _scanner.Buffer.ReadString(hashLength);
    }

    /// <inheritdoc />
    public string ReadVersion()
    {
        var versionLength = _scanner.Buffer.ReadU8();

        if (versionLength == 0)
        {
            return string.Empty;
        }

        return _scanner.Buffer.ReadString(versionLength);
    }

    /// <inheritdoc />
    public string? ReadSpineString()
    {
        var length = _scanner.Buffer.ReadLeb128I32();

        if (length <= 0)
        {
            return null;
        }

        return _scanner.Buffer.ReadString(length);
    }

    /// <inheritdoc />
    public bool ReadSpineBoolean()
    {
        return _scanner.Buffer.ReadU8() != 0;
    }

    /// <inheritdoc />
    public float ReadSpineFloat()
    {
        return _scanner.Buffer.ReadF32LE();
    }

    /// <inheritdoc />
    public (byte R, byte G, byte B, byte A) ReadSpineColor()
    {
        var r = _scanner.Buffer.ReadU8();
        var g = _scanner.Buffer.ReadU8();
        var b = _scanner.Buffer.ReadU8();
        var a = _scanner.Buffer.ReadU8();
        return (r, g, b, a);
    }

    /// <summary>
    ///     读取一个无符号 8 位整数并前进 1 字节。
    /// </summary>
    /// <returns>无符号 8 位整数值。</returns>
    public byte ReadUInt8()
    {
        return _scanner.Buffer.ReadU8();
    }

    /// <summary>
    ///     以小端序读取一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    /// <returns>无符号 32 位整数值。</returns>
    public uint ReadUInt32LittleEndian()
    {
        return _scanner.Buffer.ReadU32LE();
    }

    /// <summary>
    ///     读取一个 LEB128 编码的有符号 32 位整数。
    /// </summary>
    /// <returns>解码后的有符号 32 位整数值。</returns>
    public int ReadLeb128Int32()
    {
        return _scanner.Buffer.ReadLeb128I32();
    }
}
