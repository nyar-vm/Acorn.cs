using System.Buffers.Binary;
using System.Text;
using Acorn.FlatBuffers.Data;

namespace Acorn.FlatBuffers.Encode;

/// <summary>
///     FlatBuffers 编码器，将 <see cref="FlatBufferData" /> 编码为 FlatBuffers 二进制格式。
/// </summary>
public sealed class FlatBuffersEncoder
{
    /// <summary>
    ///     将 FlatBuffer 数据编码为二进制字节数组。
    /// </summary>
    /// <param name="data">FlatBuffer 数据，包含根表和可选文件标识符。</param>
    /// <returns>FlatBuffers 二进制数据。</returns>
    public byte[] Encode(FlatBufferData data)
    {
        var fields = data.RootTable.Fields;

        return EncodeTable(fields, data.FileIdentifier);
    }

    /// <summary>
    ///     将单个表编码为 FlatBuffers 二进制。
    /// </summary>
    /// <param name="table">表数据。</param>
    /// <param name="fileIdentifier">可选文件标识符。</param>
    /// <returns>FlatBuffers 二进制数据。</returns>
    public byte[] EncodeTable(FlatBufferTable table, string? fileIdentifier = null)
    {
        return EncodeTable(table.Fields, fileIdentifier);
    }

    private static byte[] EncodeTable(IReadOnlyList<FlatBufferField> fields, string? fileIdentifier)
    {
        var hasFileId = !string.IsNullOrEmpty(fileIdentifier);
        var headerSize = hasFileId ? 8 : 4;

        // 计算每个字段的序列化数据和大小
        var fieldInfos = new (int size, byte[]? data)[fields.Count];
        var maxOffset = 0;

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var size = GetTypeSize(field.Type);
            var serialized = SerializeValue(field.Value, field.Type);

            fieldInfos[i] = (size, serialized);

            var endOffset = field.VTableOffset + size;

            if (endOffset > maxOffset)
            {
                maxOffset = endOffset;
            }
        }

        // VTable 布局：[大小:2][对象大小:2][字段偏移0:2]...[字段偏移N:2]
        var vtableSize = FlatBuffersConstants.VTableFieldStart + fields.Count * FlatBuffersConstants.VTableOffsetSize;
        var objectSize = maxOffset;
        var tableDataSize = FlatBuffersConstants.FileIdentifierSize + objectSize; // soffset(4) + field data

        var totalSize = headerSize + vtableSize + tableDataSize;
        var buffer = new byte[totalSize];
        var pos = headerSize;

        // 写入 VTable
        var vtableStart = pos;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), (ushort)vtableSize);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), (ushort)objectSize);
        pos += 2;

        for (var i = 0; i < fields.Count; i++)
        {
            var voffset = (ushort)fields[i].VTableOffset;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), voffset);
            pos += 2;
        }

        // 写入 Table
        var tableStart = pos;

        // soffset 指向 VTable（相对于 table 起始的偏移）
        var soffset = vtableStart - tableStart;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), soffset);
        pos += 4;

        // 写入各字段数据
        for (var i = 0; i < fields.Count; i++)
        {
            var (size, data) = fieldInfos[i];

            if (data != null)
            {
                var fieldPos = tableStart + fields[i].VTableOffset;
                Array.Copy(data, 0, buffer, fieldPos, data.Length);
            }
        }

        // 写入根偏移（位置 0）
        var rootOffset = (uint)(tableStart - 0);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0), rootOffset);

        // 写入文件标识符
        if (hasFileId)
        {
            var fileId = fileIdentifier!.PadRight(4).Substring(0, 4);
            Encoding.ASCII.GetBytes(fileId, 0, 4, buffer, 4);
        }

        return buffer;
    }

    /// <summary>
    ///     获取字段类型对应的字节大小。
    /// </summary>
    private static int GetTypeSize(FlatBufferFieldType type)
    {
        return type switch
        {
            FlatBufferFieldType.Bool or FlatBufferFieldType.Byte or FlatBufferFieldType.UByte => 1,
            FlatBufferFieldType.Short or FlatBufferFieldType.UShort => 2,
            FlatBufferFieldType.Int or FlatBufferFieldType.UInt or FlatBufferFieldType.Float => 4,
            FlatBufferFieldType.Long or FlatBufferFieldType.ULong or FlatBufferFieldType.Double => 8,
            _ => 4
        };
    }

    /// <summary>
    ///     将字段值序列化为字节数组。
    /// </summary>
    private static byte[]? SerializeValue(object? value, FlatBufferFieldType type)
    {
        if (value == null)
        {
            return null;
        }

        return type switch
        {
            FlatBufferFieldType.Bool => [(byte)((bool)value ? 1 : 0)],
            FlatBufferFieldType.Byte => [(byte)(sbyte)value],
            FlatBufferFieldType.UByte => [(byte)value],
            FlatBufferFieldType.Short => BitConverter.GetBytes((short)value),
            FlatBufferFieldType.UShort => BitConverter.GetBytes((ushort)value),
            FlatBufferFieldType.Int => BitConverter.GetBytes((int)value),
            FlatBufferFieldType.UInt => BitConverter.GetBytes((uint)value),
            FlatBufferFieldType.Float => BitConverter.GetBytes((float)value),
            FlatBufferFieldType.Long => BitConverter.GetBytes((long)value),
            FlatBufferFieldType.ULong => BitConverter.GetBytes((ulong)value),
            FlatBufferFieldType.Double => BitConverter.GetBytes((double)value),
            _ => null
        };
    }
}
