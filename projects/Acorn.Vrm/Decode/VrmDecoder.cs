using Acorn.Frame;
using Acorn.Vrm.Data;

namespace Acorn.Vrm.Decode;

/// <summary>
///     VRM 解码器，从 glTF 二进制数据中解析 VRM 扩展。
/// </summary>
public ref struct VrmDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="VrmDecoder" /> 结构的新实例。
    /// </summary>
    public VrmDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 VRM 模型数据。
    /// </summary>
    public VrmModelData Decode()
    {
        var version = DetectVrmVersion();

        return new VrmModelData
        {
            Version = version
        };
    }

    /// <summary>
    ///     检测 VRM 版本。
    /// </summary>
    public VrmVersion DetectVrmVersion()
    {
        if (_buffer.Length < 4)
        {
            return VrmVersion.Unknown;
        }

        var magic = _buffer.ReadString(4);

        if (magic == "glTF")
        {
            return VrmVersion.Vrm0;
        }

        return VrmVersion.Unknown;
    }
}
