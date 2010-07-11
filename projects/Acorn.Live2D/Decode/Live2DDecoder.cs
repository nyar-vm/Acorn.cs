using System.Text;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Decode;

/// <summary>
///     Live2D moc3 文件解码器，将 Live2D Cubism moc3 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     Live2D 是 Live2D Inc. 开发的参数化 2D 动画格式，moc3 是其编译后的二进制模型格式。
///     解码器解析 moc3 文件头、计数表、偏移表和各数据段。
/// </remarks>
public ref struct Live2DDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="Live2DDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">moc3 二进制数据。</param>
    public Live2DDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 moc3 文件头，提取版本和字节序信息。
    /// </summary>
    /// <returns>包含版本号、字节序标志和修订号的元组。</returns>
    public (int Version, bool IsBigEndian, int Revision) DecodeHeader()
    {
        var signature = _buffer.ReadString(4);

        if (signature != "MOC3")
        {
            throw new InvalidDataException($"moc3 文件签名无效，期望 \"MOC3\"，实际 \"{signature}\"");
        }

        var version = _buffer.ReadU8();
        var isBigEndian = _buffer.ReadU8() != 0;
        var revision = _buffer.ReadU8();

        return (version, isBigEndian, revision);
    }

    /// <summary>
    ///     解码 moc3 文件中的参数数量。
    /// </summary>
    /// <returns>参数数量。</returns>
    public int DecodeParameterCount()
    {
        _buffer.Position = 0;
        var (version, isBigEndian, _) = DecodeHeader();

        _buffer.Position = GetCountTableOffset(version);

        if (version >= 5)
        {
            _buffer.Advance(8);
        }

        _buffer.Advance(4);

        return (int)ReadEndianAwareU32(isBigEndian);
    }

    /// <summary>
    ///     解码 moc3 文件中的部件数量。
    /// </summary>
    /// <returns>部件数量。</returns>
    public int DecodePartCount()
    {
        _buffer.Position = 0;
        var (version, isBigEndian, _) = DecodeHeader();

        _buffer.Position = GetCountTableOffset(version);

        if (version >= 5)
        {
            _buffer.Advance(8);
        }

        return (int)ReadEndianAwareU32(isBigEndian);
    }

    /// <summary>
    ///     解码 moc3 文件中的绘制对象数量。
    /// </summary>
    /// <returns>绘制对象数量。</returns>
    public int DecodeDrawableCount()
    {
        _buffer.Position = 0;
        var (version, isBigEndian, _) = DecodeHeader();

        _buffer.Position = GetCountTableOffset(version);

        if (version >= 5)
        {
            _buffer.Advance(8);
        }

        _buffer.Advance(4 + 4);

        return (int)ReadEndianAwareU32(isBigEndian);
    }

    #region 私有解析方法

    private uint ReadEndianAwareU32(bool isBigEndian)
    {
        return isBigEndian ? _buffer.ReadU32BE() : _buffer.ReadU32LE();
    }

    private static int GetCountTableOffset(int version)
    {
        return 64;
    }

    #endregion
}
