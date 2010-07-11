using System.Text;
using Acorn.Frame;

namespace Acorn.Spine.Scanner;

/// <summary>
///     Spine 格式扫描器的默认实现，基于 <see cref="ByteBuffer" /> 提供零分配的快速数据扫描。
/// </summary>
public ref struct SpineScanner : ISpineScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="SpineScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 Spine 二进制数据。</param>
    public SpineScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <inheritdoc />
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <inheritdoc />
    public int Length => _buffer.Length;

    /// <inheritdoc />
    public bool IsEndOfData => _buffer.IsEnd;

    /// <inheritdoc />
    public ReadOnlySpan<byte> Remaining => _buffer.RemainingSpan;

    /// <inheritdoc />
    public void Advance(int count)
    {
        _buffer.Advance(count);
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Peek(int count)
    {
        return _buffer.Peek(count);
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Read(int count)
    {
        return _buffer.ReadBytes(count);
    }

    /// <inheritdoc />
    public bool MatchMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.MatchMagic(magic);
    }

    /// <inheritdoc />
    public bool ConsumeMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.ConsumeMagic(magic);
    }

    /// <inheritdoc />
    public string ReadHash()
    {
        var hashLength = _buffer.ReadU8();

        if (hashLength == 0)
        {
            return string.Empty;
        }

        return _buffer.ReadString(hashLength);
    }

    /// <inheritdoc />
    public string ReadVersion()
    {
        var versionLength = _buffer.ReadU8();

        if (versionLength == 0)
        {
            return string.Empty;
        }

        return _buffer.ReadString(versionLength);
    }

    /// <inheritdoc />
    public string? ReadSpineString()
    {
        var length = _buffer.ReadLeb128I32();

        if (length <= 0)
        {
            return null;
        }

        return _buffer.ReadString(length);
    }

    /// <inheritdoc />
    public bool ReadSpineBoolean()
    {
        return _buffer.ReadU8() != 0;
    }

    /// <inheritdoc />
    public float ReadSpineFloat()
    {
        return _buffer.ReadF32LE();
    }

    /// <inheritdoc />
    public (byte R, byte G, byte B, byte A) ReadSpineColor()
    {
        var r = _buffer.ReadU8();
        var g = _buffer.ReadU8();
        var b = _buffer.ReadU8();
        var a = _buffer.ReadU8();
        return (r, g, b, a);
    }

    /// <summary>
    ///     读取一个无符号 8 位整数并前进 1 字节。
    /// </summary>
    /// <returns>无符号 8 位整数值。</returns>
    public byte ReadUInt8()
    {
        return _buffer.ReadU8();
    }

    /// <summary>
    ///     以小端序读取一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    /// <returns>无符号 32 位整数值。</returns>
    public uint ReadUInt32LittleEndian()
    {
        return _buffer.ReadU32LE();
    }

    /// <summary>
    ///     读取一个 LEB128 编码的有符号 32 位整数。
    /// </summary>
    /// <returns>解码后的有符号 32 位整数值。</returns>
    public int ReadLeb128Int32()
    {
        return _buffer.ReadLeb128I32();
    }
}
