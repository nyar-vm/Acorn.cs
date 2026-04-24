using Acorn.Frame;
using Acorn.Usd.Data;

namespace Acorn.Usd.Decode;

/// <summary>
///     USD 二进制解码器。
/// </summary>
public ref struct UsdDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="UsdDecoder" /> 结构的新实例。
    /// </summary>
    public UsdDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 USDC 文件。
    /// </summary>
    public UsdStageData Decode()
    {
        var magic = _buffer.ReadString(UsdConstants.MagicLength);

        if (magic != "PXR-USDC")
        {
            throw new InvalidDataException($"USDC 文件签名无效，期望 \"PXR-USDC\"，实际 \"{magic}\"");
        }

        var version = (int)_buffer.ReadU32LE();
        _buffer.Advance(UsdConstants.HeaderSize - 12);

        return new UsdStageData
        {
            FileType = UsdFileType.Crate,
            Version = version
        };
    }
}
