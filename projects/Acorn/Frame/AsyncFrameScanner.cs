using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Acorn.Frame;

/// <summary>
///     异步帧扫描器，基于 <see cref="PipeReader" /> 提供异步的帧级扫描能力。
/// </summary>
/// <typeparam name="TProtocol">协议实现类型，必须实现 <see cref="IFrameProtocol" />。</typeparam>
public ref struct AsyncFrameScanner<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private readonly PipeReader _reader;
    private readonly TProtocol _protocol;
    private ReadOnlySequence<byte> _buffer;

    public AsyncFrameScanner(PipeReader reader, TProtocol protocol = default)
    {
        _reader = reader;
        _protocol = protocol;
        _buffer = ReadOnlySequence<byte>.Empty;
    }

    public ValueTask<ReadResult> ReadNextAsync(CancellationToken ct = default)
    {
        return _reader.ReadAsync(ct);
    }

    public bool TryGetNextFrame(out Frame frame)
    {
        frame = default;

        if (_buffer.IsEmpty)
        {
            return false;
        }

        var span = _buffer.IsSingleSegment
            ? _buffer.FirstSpan
            : _buffer.ToArray();

        if (!_protocol.TryReadFrame(span, out frame))
        {
            return false;
        }

        _buffer = _buffer.Slice(frame.Size);
        return true;
    }

    public void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _reader.AdvanceTo(consumed, examined);
    }

    public void UpdateBuffer(ReadOnlySequence<byte> buffer)
    {
        _buffer = buffer;
    }
}
