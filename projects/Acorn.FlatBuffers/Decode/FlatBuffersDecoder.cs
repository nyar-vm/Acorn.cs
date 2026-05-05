using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.FlatBuffers.Data;

namespace Acorn.FlatBuffers.Decode;

/// <summary>
///     FlatBuffers 解码器。
/// </summary>
public ref struct FlatBuffersDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="FlatBuffersDecoder" /> 结构的新实例。
    /// </summary>
    public FlatBuffersDecoder(ReadOnlySpan<byte> data)
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
    ///     解码根表偏移。
    /// </summary>
    public uint DecodeRootOffset()
    {
        return _buffer.ReadU32LE();
    }

    /// <summary>
    ///     解码指定偏移处的表。
    /// </summary>
    public FlatBufferTable DecodeTable(uint offset)
    {
        _buffer.Position = (int)offset;

        var vtableOffset = (int)(_buffer.ReadI32LE() + offset);

        var vTableSize = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Data.Slice(vtableOffset + FlatBuffersConstants.VTableSizeOffset));
        var objectSize = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Data.Slice(vtableOffset + FlatBuffersConstants.VTableDataOffset));

        var fieldCount = (vTableSize - 4) / 2;
        var fields = new List<FlatBufferField>();

        for (var i = 0; i < fieldCount; i++)
        {
            var fieldVTableOffset = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Data.Slice(vtableOffset + 4 + i * 2));

            if (fieldVTableOffset == 0)
            {
                continue;
            }

            fields.Add(new FlatBufferField
            {
                Index = i,
                VTableOffset = fieldVTableOffset
            });
        }

        return new FlatBufferTable
        {
            TableOffset = offset,
            VTableOffset = (uint)vtableOffset,
            VTableSize = vTableSize,
            Fields = fields
        };
    }

    /// <summary>
    ///     读取文件标识符。
    /// </summary>
    public string ReadFileIdentifier()
    {
        if (_buffer.Length < 8)
        {
            return string.Empty;
        }

        return Encoding.ASCII.GetString(_buffer.Data.Slice(4, 4));
    }
}
