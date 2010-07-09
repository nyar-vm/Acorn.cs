using AcornFrame = Acorn.Frame.Frame;
using Acorn.Frame;
using Acorn.PostgreSql.Data;

namespace Acorn.PostgreSql.Scanner;

public struct PostgreSqlProtocol : IFrameProtocol
{
    public int MinFrameSize => 5;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 5)
        {
            return false;
        }

        var messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(1, 4));
        frameSize = 1 + messageLength;
        return true;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < 5)
        {
            return false;
        }

        var messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(1, 4));
        var totalSize = 1 + messageLength;

        if (buffer.Length < totalSize)
        {
            return false;
        }

        var payload = messageLength > 4 ? buffer.Slice(5, messageLength - 4) : ReadOnlySpan<byte>.Empty;
        frame = new AcornFrame(totalSize, payload);
        return true;
    }
}

public ref struct PostgreSqlScanner
{
    private FrameScanner<PostgreSqlProtocol> _frameScanner;

    public PostgreSqlScanner(ReadOnlySpan<byte> data)
    {
        _frameScanner = new FrameScanner<PostgreSqlProtocol>(data);
    }

    public int Position => _frameScanner.Position;

    public int Length => _frameScanner.Length;

    public bool IsEndOfData => _frameScanner.IsEnd;

    public bool TryReadNext(out AcornFrame frame)
    {
        return _frameScanner.TryReadNext(out frame);
    }

    public PostgreSqlFrameStatistics ScanFrameStatistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;

        while (_frameScanner.TryReadNext(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.Payload.Length;
        }

        return new PostgreSqlFrameStatistics
        {
            TotalFrames = totalFrames,
            TotalPayloadBytes = totalPayloadBytes
        };
    }
}

public sealed class PostgreSqlFrameStatistics
{
    public int TotalFrames { get; init; }
    public long TotalPayloadBytes { get; init; }
}
