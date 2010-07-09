using System.Text;
using Acorn.Frame;

namespace Acorn.Protobuf.Scanner;

/// <summary>
///     Protobuf 格式扫描器，基于 <see cref="ByteBuffer" /> 提供零分配的快速 Protobuf 消息结构探查。
/// </summary>
/// <remarks>
///     Protobuf 使用变长编码（Varint）和标签-值对（Tag-Length-Value）格式组织数据。
///     扫描器逐字段解析消息结构，提取字段编号、线型和值摘要。
/// </remarks>
public ref struct ProtobufScanner
{
    private ByteBuffer _buffer;

    public ProtobufScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public int Length => _buffer.Length;

    public bool IsEndOfData => _buffer.IsEnd;

    public ReadOnlySpan<byte> Remaining => _buffer.RemainingSpan;

    public void Advance(int count)
    {
        _buffer.Advance(count);
    }

    public ReadOnlySpan<byte> Peek(int count)
    {
        return _buffer.Peek(count);
    }

    public ReadOnlySpan<byte> Read(int count)
    {
        return _buffer.ReadBytes(count);
    }

    public bool MatchMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.MatchMagic(magic);
    }

    public bool ConsumeMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.ConsumeMagic(magic);
    }

    /// <summary>
    ///     扫描 Protobuf 消息，提取所有顶层字段信息。
    /// </summary>
    /// <returns>字段信息列表。</returns>
    public List<ProtobufFieldInfo> ScanFields()
    {
        var fields = new List<ProtobufFieldInfo>();

        while (!_buffer.IsEnd)
        {
            if (!TryReadTag(out var fieldNumber, out var wireType))
            {
                break;
            }

            var field = new ProtobufFieldInfo
            {
                FieldNumber = fieldNumber,
                WireType = wireType
            };

            switch (wireType)
            {
                case 0:
                    field = field with
                    {
                        ValueType = ProtobufValueType.Varint, VarintValue = ReadVarint()
                    };
                    break;
                case 1:
                    field = field with
                    {
                        ValueType = ProtobufValueType.Fixed64, Fixed64Value = _buffer.ReadU64LE()
                    };
                    break;
                case 2:
                    field = field with
                    {
                        ValueType = ProtobufValueType.LengthDelimited,
                        LengthDelimitedValue = _buffer.ReadBytes((int)ReadVarint()).ToArray()
                    };
                    break;
                case 5:
                    field = field with
                    {
                        ValueType = ProtobufValueType.Fixed32, Fixed32Value = _buffer.ReadU32LE()
                    };
                    break;
                default:
                    return fields;
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    ///     扫描 Protobuf 消息结构，返回文本描述。
    /// </summary>
    /// <returns>消息结构描述。</returns>
    public string ScanStructure()
    {
        var result = new StringBuilder();
        result.AppendLine("Protobuf Message Structure:");
        result.AppendLine("==========================");

        var fields = ScanFields();

        foreach (var field in fields)
        {
            var wireTypeName = field.WireType switch
            {
                0 => "Varint",
                1 => "64-bit",
                2 => "Length-delimited",
                5 => "32-bit",
                _ => $"Unknown({field.WireType})"
            };

            var valueDesc = field.ValueType switch
            {
                ProtobufValueType.Varint => field.VarintValue.ToString(),
                ProtobufValueType.Fixed64 => $"0x{field.Fixed64Value:X16}",
                ProtobufValueType.Fixed32 => $"0x{field.Fixed32Value:X8}",
                ProtobufValueType.LengthDelimited => TryDecodeString(field.LengthDelimitedValue, out var str)
                    ? $"\"{str}\""
                    : $"[{field.LengthDelimitedValue.Length} bytes]",
                _ => "unknown"
            };

            result.AppendLine($"  Field {field.FieldNumber} ({wireTypeName}): {valueDesc}");
        }

        return result.ToString();
    }

    private bool TryReadTag(out int fieldNumber, out int wireType)
    {
        wireType = 0;
        fieldNumber = 0;

        if (_buffer.IsEnd)
        {
            return false;
        }

        var tag = ReadVarint();

        if (tag == 0)
        {
            return false;
        }

        wireType = (int)(tag & 0x7);
        fieldNumber = (int)(tag >> 3);
        return true;
    }

    private ulong ReadVarint()
    {
        return _buffer.ReadLeb128U64();
    }

    private static bool TryDecodeString(byte[] bytes, out string value)
    {
        value = string.Empty;

        try
        {
            var decoded = Encoding.UTF8.GetString(bytes);

            foreach (var c in decoded)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                {
                    return false;
                }
            }

            value = decoded;
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     Protobuf 字段信息。
/// </summary>
public sealed record ProtobufFieldInfo
{
    /// <summary>
    ///     字段编号。
    /// </summary>
    public int FieldNumber { get; init; }

    /// <summary>
    ///     线型。
    /// </summary>
    public int WireType { get; init; }

    /// <summary>
    ///     值类型。
    /// </summary>
    public ProtobufValueType ValueType { get; init; }

    /// <summary>
    ///     Varint 值（当 <see cref="ValueType" /> 为 <see cref="ProtobufValueType.Varint" /> 时有效）。
    /// </summary>
    public ulong VarintValue { get; init; }

    /// <summary>
    ///     32 位固定值（当 <see cref="ValueType" /> 为 <see cref="ProtobufValueType.Fixed32" /> 时有效）。
    /// </summary>
    public uint Fixed32Value { get; init; }

    /// <summary>
    ///     64 位固定值（当 <see cref="ValueType" /> 为 <see cref="ProtobufValueType.Fixed64" /> 时有效）。
    /// </summary>
    public ulong Fixed64Value { get; init; }

    /// <summary>
    ///     变长值（当 <see cref="ValueType" /> 为 <see cref="ProtobufValueType.LengthDelimited" /> 时有效）。
    /// </summary>
    public byte[] LengthDelimitedValue { get; init; } = [];
}

/// <summary>
///     Protobuf 值类型。
/// </summary>
public enum ProtobufValueType
{
    /// <summary>
    ///     变长整数。
    /// </summary>
    Varint,

    /// <summary>
    ///     32 位固定值。
    /// </summary>
    Fixed32,

    /// <summary>
    ///     64 位固定值。
    /// </summary>
    Fixed64,

    /// <summary>
    ///     变长数据。
    /// </summary>
    LengthDelimited
}
