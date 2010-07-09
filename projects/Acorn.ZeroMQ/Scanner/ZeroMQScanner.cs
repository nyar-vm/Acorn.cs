using AcornFrame = Acorn.Frame.Frame;
using Acorn.Frame;
using Acorn.ZeroMQ.Data;

namespace Acorn.ZeroMQ.Scanner;

public struct ZeroMQProtocol : IFrameProtocol
{
    public int MinFrameSize => 9;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 9)
        {
            return false;
        }

        var payloadLength = (long)System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(1, 8));

        if (payloadLength > int.MaxValue)
        {
            frameSize = 0;
            return false;
        }

        frameSize = 9 + (int)payloadLength;
        return true;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < 9)
        {
            return false;
        }

        var payloadLength = (long)System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(1, 8));

        if (payloadLength > int.MaxValue)
        {
            return false;
        }

        var totalSize = 9 + (int)payloadLength;

        if (buffer.Length < totalSize)
        {
            return false;
        }

        var payload = payloadLength > 0 ? buffer.Slice(9, (int)payloadLength) : ReadOnlySpan<byte>.Empty;
        frame = new AcornFrame(totalSize, payload);
        return true;
    }
}

public ref struct ZeroMQScanner
{
    private FrameScanner<ZeroMQProtocol> _frameScanner;

    public ZeroMQScanner(ReadOnlySpan<byte> data)
    {
        _frameScanner = new FrameScanner<ZeroMQProtocol>(data);
    }

    public int Position => _frameScanner.Position;

    public int Length => _frameScanner.Length;

    public bool IsEndOfData => _frameScanner.IsEnd;

    public bool TryReadNext(out AcornFrame frame)
    {
        return _frameScanner.TryReadNext(out frame);
    }

    public ZeroMQFrameStatistics ScanFrameStatistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;
        var multiPartMessages = 0;
        var isMultiPart = false;

        while (_frameScanner.TryReadNext(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.Payload.Length;

            var flags = _frameScanner.Buffer.RemainingSpan.Length >= 0
                ? 0
                : 0;

            var hasMore = (flags & 0x01) != 0;

            if (!isMultiPart && hasMore)
            {
                multiPartMessages++;
                isMultiPart = true;
            }

            if (!hasMore)
            {
                isMultiPart = false;
            }
        }

        return new ZeroMQFrameStatistics
        {
            TotalFrames = totalFrames,
            TotalPayloadBytes = totalPayloadBytes,
            MultiPartMessages = multiPartMessages
        };
    }
}

public sealed class ZeroMQFrameStatistics
{
    public int TotalFrames { get; init; }
    public long TotalPayloadBytes { get; init; }
    public int MultiPartMessages { get; init; }
}
