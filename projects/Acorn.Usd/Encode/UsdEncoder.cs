using Acorn.Usd.Data;

namespace Acorn.Usd.Encode;

/// <summary>
///     USD 二进制编码器，将 <see cref="UsdStageData" /> 编码为 USDC 二进制格式。
/// </summary>
public sealed class UsdEncoder
{
    /// <summary>
    ///     将 USD 场景数据编码为 USDC 二进制。
    /// </summary>
    /// <param name="data">USD 场景数据。</param>
    /// <returns>USDC 二进制数据。</returns>
    public byte[] Encode(UsdStageData data)
    {
        var buffer = new byte[UsdConstants.HeaderSize];
        var pos = 0;

        // 魔数 "PXR-USDC"
        "PXR-USDC"u8.CopyTo(buffer.AsSpan(pos));
        pos += UsdConstants.MagicLength;

        // 版本号
        WriteU32LE(buffer, ref pos, (uint)data.Version);

        // 剩余头部填零
        while (pos < UsdConstants.HeaderSize)
        {
            buffer[pos++] = 0;
        }

        return buffer;
    }

    /// <summary>
    ///     写入 UInt32 Little-Endian。
    /// </summary>
    private static void WriteU32LE(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)(value);
        buffer[pos++] = (byte)(value >> 8);
        buffer[pos++] = (byte)(value >> 16);
        buffer[pos++] = (byte)(value >> 24);
    }
}
