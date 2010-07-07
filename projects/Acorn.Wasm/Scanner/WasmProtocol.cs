using AcornFrame = Acorn.Frame.Frame;
using Acorn.Frame;

namespace Acorn.Wasm.Scanner;

/// <summary>
///     Wasm 段协议，实现 <see cref="IFrameProtocol" /> 以支持段帧扫描。
/// </summary>
/// <remarks>
///     Wasm 段帧格式：1 字节段 ID + LEB128 无符号 32 位段大小 + 段内容。
/// </remarks>
public struct WasmProtocol : IFrameProtocol
{
    /// <summary>
    ///     Wasm 段帧的最小大小（1 字节段 ID + 1 字节 LEB128 段大小）。
    /// </summary>
    public int MinFrameSize => 2;

    /// <summary>
    ///     尝试从缓冲区预读 Wasm 段帧大小，不消耗任何数据。
    /// </summary>
    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < MinFrameSize)
        {
            return false;
        }

        var offset = 1;
        uint sectionSize = 0;
        var shift = 0;

        while (true)
        {
            if (offset >= buffer.Length)
            {
                return false;
            }

            var b = buffer[offset++];
            sectionSize |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;

            if (shift >= 32)
            {
                return false;
            }
        }

        frameSize = offset + (int)sectionSize;
        return true;
    }

    /// <summary>
    ///     尝试从缓冲区读取 Wasm 段帧。
    /// </summary>
    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < MinFrameSize)
        {
            return false;
        }

        var offset = 1;
        uint sectionSize = 0;
        var shift = 0;

        while (true)
        {
            if (offset >= buffer.Length)
            {
                return false;
            }

            var b = buffer[offset++];
            sectionSize |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;

            if (shift >= 32)
            {
                return false;
            }
        }

        var headerSize = offset;
        var totalSize = headerSize + (int)sectionSize;

        if (buffer.Length < totalSize)
        {
            return false;
        }

        var raw = buffer.Slice(0, totalSize);
        var payload = buffer.Slice(headerSize, (int)sectionSize);
        frame = new AcornFrame(totalSize, payload, raw);
        return true;
    }
}
