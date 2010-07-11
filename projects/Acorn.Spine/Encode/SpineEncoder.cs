using System.Text;
using Acorn.Frame;

namespace Acorn.Spine.Encode;

/// <summary>
///     Spine 二进制编码器，将 C# 数据结构编码为 Spine 二进制格式。
/// </summary>
/// <remarks>
///     Spine 二进制格式使用小端序存储数值，字符串使用长度前缀（LEB128 编码的 int）加 UTF-8 字节的方式存储。
///     编码器支持骨骼、插槽、附件、动画等 Spine 核心数据结构。
/// </remarks>
public ref struct SpineEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始化 <see cref="SpineEncoder" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    public SpineEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position => _writer.Position;

    /// <summary>
    ///     写入文件头魔数 "skeleton" 的 ASCII 字节。
    /// </summary>
    public void WriteHeader()
    {
        var magic = Encoding.ASCII.GetBytes("skeleton");
        _writer.Write(magic);
    }

    /// <summary>
    ///     写入哈希值字符串。
    /// </summary>
    /// <param name="hash">哈希字符串。</param>
    public void WriteHash(string? hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            _writer.WriteU8(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(hash);
        _writer.WriteU8((byte)bytes.Length);
        _writer.Write(bytes);
    }

    /// <summary>
    ///     写入版本号字符串。
    /// </summary>
    /// <param name="version">版本号字符串。</param>
    public void WriteVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            _writer.WriteU8(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(version);
        _writer.WriteU8((byte)bytes.Length);
        _writer.Write(bytes);
    }

    /// <summary>
    ///     写入 Spine 变长字符串。
    /// </summary>
    /// <param name="value">要写入的字符串，null 或空字符串会写入长度 0。</param>
    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _writer.WriteLeb128I32(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        _writer.WriteLeb128I32(bytes.Length);
        _writer.Write(bytes);
    }

    /// <summary>
    ///     写入 Spine 布尔值。
    /// </summary>
    /// <param name="value">布尔值。</param>
    public void WriteBoolean(bool value)
    {
        _writer.WriteU8(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    ///     写入单精度浮点数（小端序）。
    /// </summary>
    /// <param name="value">浮点数值。</param>
    public void WriteFloat(float value)
    {
        _writer.WriteF32LE(value);
    }

    /// <summary>
    ///     写入颜色值（4 字节 RGBA）。
    /// </summary>
    /// <param name="r">红色分量。</param>
    /// <param name="g">绿色分量。</param>
    /// <param name="b">蓝色分量。</param>
    /// <param name="a">透明度分量。</param>
    public void WriteColor(byte r, byte g, byte b, byte a)
    {
        _writer.WriteU8(r);
        _writer.WriteU8(g);
        _writer.WriteU8(b);
        _writer.WriteU8(a);
    }

    /// <summary>
    ///     写入无符号 8 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt8(byte value)
    {
        _writer.WriteU8(value);
    }

    /// <summary>
    ///     写入无符号 16 位整数（小端序）。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt16(ushort value)
    {
        _writer.WriteU16LE(value);
    }

    /// <summary>
    ///     写入无符号 32 位整数（小端序）。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt32(uint value)
    {
        _writer.WriteU32LE(value);
    }

    /// <summary>
    ///     写入有符号 32 位整数（小端序）。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteInt32(int value)
    {
        _writer.WriteI32LE(value);
    }

    /// <summary>
    ///     写入 LEB128 编码的有符号 32 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteLeb128Int32(int value)
    {
        _writer.WriteLeb128I32(value);
    }
}
