namespace Acorn;

/// <summary>
///     文件格式检测工具。基于魔数（Magic Bytes）提供统一的二进制格式检测入口。
/// </summary>
public static class FileDetector
{
    /// <summary>
    ///     检测输入字节数据的文件格式。
    /// </summary>
    /// <param name="header">文件头部字节数据。</param>
    /// <returns>格式标识字符串：<c>"ELF"</c>、<c>"PE"</c>、<c>"WASM"</c> 或 <c>"Unknown"</c>。</returns>
    public static string Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length < 4)
        {
            return "Unknown";
        }

        if (header[0] == 0x7F && header[1] == (byte)'E' && header[2] == (byte)'L' && header[3] == (byte)'F')
        {
            return "ELF";
        }

        if (header[0] == 0x00 && header[1] == (byte)'a' && header[2] == (byte)'s' && header[3] == (byte)'m')
        {
            return "WASM";
        }

        if (header[0] == (byte)'M' && header[1] == (byte)'Z')
        {
            return "PE";
        }

        return "Unknown";
    }
}
