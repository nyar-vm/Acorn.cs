using System.Text;
using Acorn.Frame;

namespace Acorn.Spine.Decode;

/// <summary>
///     Spine 二进制解码器，将 Spine 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     Spine 二进制格式使用小端序存储数值，字符串使用长度前缀（LEB128 编码的 int）加 UTF-8 字节的方式存储。
///     解码器支持骨骼、插槽、附件、动画等 Spine 核心数据结构。
/// </remarks>
public ref struct SpineDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="SpineDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要解码的 Spine 二进制数据。</param>
    public SpineDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position => _buffer.Position;

    /// <summary>
    ///     获取流的总长度。
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     获取一个值，该值指示是否已到达流的末尾。
    /// </summary>
    public bool IsEndOfStream => _buffer.IsEnd;

    /// <summary>
    ///     读取并验证文件头魔数。
    /// </summary>
    /// <returns>如果魔数匹配则返回 true，否则返回 false。</returns>
    public bool ReadHeader()
    {
        var expectedMagic = Encoding.ASCII.GetBytes("skeleton");
        var actualMagic = _buffer.ReadBytes(expectedMagic.Length).ToArray();
        return actualMagic.SequenceEqual(expectedMagic);
    }

    /// <summary>
    ///     读取哈希值字符串。
    /// </summary>
    /// <returns>哈希字符串，如果不存在则返回空字符串。</returns>
    public string ReadHash()
    {
        var hashLength = _buffer.ReadU8();

        if (hashLength == 0)
        {
            return string.Empty;
        }

        return _buffer.ReadString(hashLength);
    }

    /// <summary>
    ///     读取版本号字符串。
    /// </summary>
    /// <returns>版本号字符串，如果不存在则返回空字符串。</returns>
    public string ReadVersion()
    {
        var versionLength = _buffer.ReadU8();

        if (versionLength == 0)
        {
            return string.Empty;
        }

        return _buffer.ReadString(versionLength);
    }

    /// <summary>
    ///     读取 Spine 变长字符串。
    /// </summary>
    /// <returns>读取的字符串，如果长度为 0 或负数则返回 null。</returns>
    public string? ReadString()
    {
        var length = _buffer.ReadLeb128I32();

        if (length <= 0)
        {
            return null;
        }

        return _buffer.ReadString(length);
    }

    /// <summary>
    ///     读取 Spine 布尔值。
    /// </summary>
    /// <returns>布尔值。</returns>
    public bool ReadBoolean()
    {
        return _buffer.ReadU8() != 0;
    }

    /// <summary>
    ///     读取单精度浮点数（小端序）。
    /// </summary>
    /// <returns>浮点数值。</returns>
    public float ReadFloat()
    {
        return _buffer.ReadF32LE();
    }

    /// <summary>
    ///     读取颜色值（4 字节 RGBA）。
    /// </summary>
    /// <returns>包含 R、G、B、A 四个分量的元组。</returns>
    public (byte R, byte G, byte B, byte A) ReadColor()
    {
        var r = _buffer.ReadU8();
        var g = _buffer.ReadU8();
        var b = _buffer.ReadU8();
        var a = _buffer.ReadU8();
        return (r, g, b, a);
    }

    /// <summary>
    ///     读取无符号 8 位整数。
    /// </summary>
    /// <returns>无符号 8 位整数值。</returns>
    public byte ReadUInt8()
    {
        return _buffer.ReadU8();
    }

    /// <summary>
    ///     读取无符号 16 位整数（小端序）。
    /// </summary>
    /// <returns>无符号 16 位整数值。</returns>
    public ushort ReadUInt16()
    {
        return _buffer.ReadU16LE();
    }

    /// <summary>
    ///     读取无符号 32 位整数（小端序）。
    /// </summary>
    /// <returns>无符号 32 位整数值。</returns>
    public uint ReadUInt32()
    {
        return _buffer.ReadU32LE();
    }

    /// <summary>
    ///     读取有符号 32 位整数（小端序）。
    /// </summary>
    /// <returns>有符号 32 位整数值。</returns>
    public int ReadInt32()
    {
        return _buffer.ReadI32LE();
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 32 位整数。
    /// </summary>
    /// <returns>解码后的有符号 32 位整数值。</returns>
    public int ReadLeb128Int32()
    {
        return _buffer.ReadLeb128I32();
    }

    /// <summary>
    ///     设置流中的位置。
    /// </summary>
    /// <param name="offset">字节偏移量。</param>
    public void Seek(int offset)
    {
        _buffer.Position = offset;
    }
}
