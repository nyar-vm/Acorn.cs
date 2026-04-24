using Acorn.Frame;
using Acorn.Usd.Data;

namespace Acorn.Usd.Scanner;

/// <summary>
///     USD 扫描器。
/// </summary>
public ref struct UsdScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="UsdScanner" /> 结构的新实例。
    /// </summary>
    public UsdScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     扫描 USD 文件头。
    /// </summary>
    public UsdScanHeader ScanHeader()
    {
        if (_buffer.Length < UsdConstants.HeaderSize)
        {
            return new UsdScanHeader { FileType = UsdFileType.Unknown };
        }

        if (_buffer.MatchMagic(UsdConstants.UsdcMagic))
        {
            _buffer.ConsumeMagic(UsdConstants.UsdcMagic);
            var version = (int)_buffer.ReadU32LE();

            return new UsdScanHeader
            {
                FileType = UsdFileType.Crate,
                Version = version
            };
        }

        return new UsdScanHeader { FileType = UsdFileType.Unknown };
    }

    /// <summary>
    ///     快速判断是否为 USDC 格式。
    /// </summary>
    public bool IsUsdc()
    {
        return _buffer.Length >= UsdConstants.MagicLength && _buffer.MatchMagic(UsdConstants.UsdcMagic);
    }
}

/// <summary>
///     USD 扫描头部信息。
/// </summary>
public sealed class UsdScanHeader
{
    /// <summary>
    ///     文件类型。
    /// </summary>
    public UsdFileType FileType { get; init; }

    /// <summary>
    ///     版本号。
    /// </summary>
    public int Version { get; init; }
}
