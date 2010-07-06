using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Acorn.Frame;

/// <summary>
///     帧构建器，基于 <see cref="PipeWriter" /> 提供对称的帧写入能力。
/// </summary>
/// <typeparam name="TProtocol">协议实现类型，必须实现 <see cref="IFrameProtocol" />。</typeparam>
public ref struct FrameBuilder<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private readonly PipeWriter _writer;
    private readonly TProtocol _protocol;

    public FrameBuilder(PipeWriter writer, TProtocol protocol = default)
    {
        _writer = writer;
        _protocol = protocol;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        return _writer.GetSpan(sizeHint);
    }

    public void Advance(int bytes)
    {
        _writer.Advance(bytes);
    }

    public ValueTask<FlushResult> FlushAsync(CancellationToken ct = default)
    {
        return _writer.FlushAsync(ct);
    }

    /// <summary>
    ///     尝试开始写入帧，预留帧头空间供后续回填。
    /// </summary>
    /// <param name="headerSpace">预留的帧头空间。</param>
    /// <returns>是否成功预留空间。</returns>
    public bool TryBeginFrame(out Span<byte> headerSpace)
    {
        var headerSize = _protocol.MinFrameSize;
        headerSpace = _writer.GetSpan(headerSize);

        return headerSpace.Length >= headerSize;
    }

    /// <summary>
    ///     完成帧写入，将帧头和载荷数据写入。
    /// </summary>
    /// <param name="headerSpan">帧头数据（由 TryBeginFrame 返回的空间中填写的内容）。</param>
    /// <param name="payload">载荷数据。</param>
    public void CompleteFrame(Span<byte> headerSpan, ReadOnlySpan<byte> payload)
    {
        var headerSize = _protocol.MinFrameSize;
        var totalSize = headerSize + payload.Length;
        var output = _writer.GetSpan(totalSize);

        headerSpan.Slice(0, headerSize).CopyTo(output);
        payload.CopyTo(output.Slice(headerSize));
        _writer.Advance(totalSize);
    }
}
