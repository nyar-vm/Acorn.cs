using AcornFrame = Acorn.Frame.Frame;
using Acorn.MySql.Data;
using Acorn.Frame;

namespace Acorn.MySql.Scanner;

public struct MySqlProtocol : IFrameProtocol
{
    public int MinFrameSize => 4;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 4)
        {
            return false;
        }

        var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);
        frameSize = 4 + payloadLength;
        return true;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < 4)
        {
            return false;
        }

        var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);

        if (buffer.Length < 4 + payloadLength)
        {
            return false;
        }

        var payload = payloadLength > 0 ? buffer.Slice(4, payloadLength) : ReadOnlySpan<byte>.Empty;
        frame = new AcornFrame(4 + payloadLength, payload);
        return true;
    }
}

public ref struct MySqlScanner
{
    private FrameScanner<MySqlProtocol> _frameScanner;

    public MySqlScanner(ReadOnlySpan<byte> data)
    {
        _frameScanner = new FrameScanner<MySqlProtocol>(data);
    }

    public int Position => _frameScanner.Position;

    public int Length => _frameScanner.Length;

    public bool IsEndOfData => _frameScanner.IsEnd;

    public bool TryReadNext(out AcornFrame frame)
    {
        return _frameScanner.TryReadNext(out frame);
    }

    public MySqlFrameStatistics ScanFrameStatistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;
        var maxPayloadSize = 0;
        var minPayloadSize = int.MaxValue;

        while (_frameScanner.TryReadNext(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.Payload.Length;

            if (frame.Payload.Length > maxPayloadSize)
            {
                maxPayloadSize = frame.Payload.Length;
            }

            if (frame.Payload.Length < minPayloadSize)
            {
                minPayloadSize = frame.Payload.Length;
            }
        }

        return new MySqlFrameStatistics
        {
            TotalFrames = totalFrames,
            TotalPayloadBytes = totalPayloadBytes,
            MaxPayloadSize = totalFrames > 0 ? maxPayloadSize : 0,
            MinPayloadSize = totalFrames > 0 ? minPayloadSize : 0
        };
    }
}

public sealed class MySqlFrameStatistics
{
    public int TotalFrames { get; init; }
    public long TotalPayloadBytes { get; init; }
    public int MaxPayloadSize { get; init; }
    public int MinPayloadSize { get; init; }
}
