using System.Text;
using Acorn.Frame;
using Acorn.SafeTensors.Data;

namespace Acorn.SafeTensors.Decode;

/// <summary>
///     SafeTensors 格式解码器，将 HuggingFace SafeTensors 格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     SafeTensors 是 HuggingFace 定义的张量存储格式，使用小端序存储数值，
///     文件头为 JSON 格式，包含张量名称、数据类型、形状和偏移量等元信息。
///     解码器解析文件头 JSON 和张量数据，支持多种数据类型和形状。
/// </remarks>
public ref struct SafeTensorsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="SafeTensorsDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">SafeTensors 二进制数据。</param>
    public SafeTensorsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 SafeTensors 文件。
    /// </summary>
    /// <returns>SafeTensors 文件数据。</returns>
    public SafeTensorsFileData Decode()
    {
        var headerLength = _buffer.ReadU64LE();
        var headerJson = _buffer.ReadString((int)headerLength);
        var header = ParseHeader(headerJson);

        var dataStart = (int)(8 + headerLength);
        var dataEnd = _buffer.Length;
        var dataLength = dataEnd - dataStart;

        _buffer.Position = dataStart;
        var rawData = dataLength > 0 ? _buffer.ReadBytes(dataLength).ToArray() : [];

        return new SafeTensorsFileData
        {
            Tensors = header,
            Data = rawData
        };
    }

    /// <summary>
    ///     仅解码 SafeTensors 文件头。
    /// </summary>
    /// <returns>张量名称到元数据的映射。</returns>
    public IReadOnlyDictionary<string, SafeTensorMeta> DecodeHeader()
    {
        var headerLength = _buffer.ReadU64LE();
        var headerJson = _buffer.ReadString((int)headerLength);
        return ParseHeader(headerJson);
    }

    /// <summary>
    ///     解码指定名称的张量数据。
    /// </summary>
    /// <param name="name">张量名称。</param>
    /// <returns>张量数据，如果不存在则返回 null。</returns>
    public SafeTensorData? DecodeTensor(string name)
    {
        var headerLength = _buffer.ReadU64LE();
        var headerJson = _buffer.ReadString((int)headerLength);
        var header = ParseHeader(headerJson);

        if (!header.TryGetValue(name, out var meta))
        {
            return null;
        }

        var tensorStart = (int)(8 + (long)headerLength + meta.DataOffset);
        _buffer.Position = tensorStart;
        var tensorData = _buffer.ReadBytes((int)meta.DataLength).ToArray();

        return new SafeTensorData
        {
            Name = name,
            DType = meta.DType,
            Shape = meta.Shape,
            Data = tensorData
        };
    }

    #region 私有解析方法

    private static Dictionary<string, SafeTensorMeta> ParseHeader(string headerJson)
    {
        var result = new Dictionary<string, SafeTensorMeta>();

        var json = headerJson.Trim();
        json = json.TrimStart('{').TrimEnd('}');

        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        var parts = SplitJsonEntries(json);

        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');

            if (colonIndex < 0)
            {
                continue;
            }

            var keyJson = part[..colonIndex].Trim();
            var valueJson = part[(colonIndex + 1)..].Trim();

            if (keyJson.StartsWith('"') && keyJson.EndsWith('"'))
            {
                keyJson = keyJson[1..^1];
            }

            if (keyJson == "__metadata__")
            {
                continue;
            }

            var meta = ParseTensorMeta(valueJson);
            result[keyJson] = meta;
        }

        return result;
    }

    private static SafeTensorMeta ParseTensorMeta(string json)
    {
        var dtype = SafeTensorDType.Float32;
        var shape = (IReadOnlyList<long>)[];
        var dataOffset = 0L;
        var dataLength = 0L;

        var dtypeIndex = json.IndexOf("\"dtype\"");

        if (dtypeIndex >= 0)
        {
            var valueStart = json.IndexOf(':', dtypeIndex) + 1;
            var valueEnd = json.IndexOfAny([',', '}'], valueStart);
            var dtypeValue = json[valueStart..valueEnd].Trim().Trim('"');
            dtype = ParseDType(dtypeValue);
        }

        var shapeIndex = json.IndexOf("\"shape\"");

        if (shapeIndex >= 0)
        {
            var arrayStart = json.IndexOf('[', shapeIndex);
            var arrayEnd = json.IndexOf(']', arrayStart);
            var arrayContent = json[(arrayStart + 1)..arrayEnd];
            shape = ParseShape(arrayContent);
        }

        var offsetsIndex = json.IndexOf("\"data_offsets\"");

        if (offsetsIndex >= 0)
        {
            var arrayStart = json.IndexOf('[', offsetsIndex);
            var arrayEnd = json.IndexOf(']', arrayStart);
            var arrayContent = json[(arrayStart + 1)..arrayEnd];
            var offsets = ParseDataOffsets(arrayContent);

            if (offsets.Length >= 2)
            {
                dataOffset = offsets[0];
                dataLength = offsets[1] - offsets[0];
            }
        }

        return new SafeTensorMeta
        {
            DType = dtype,
            Shape = shape,
            DataOffset = dataOffset,
            DataLength = dataLength
        };
    }

    private static SafeTensorDType ParseDType(string dtype)
    {
        return dtype switch
        {
            "BOOL" => SafeTensorDType.Bool,
            "U8" => SafeTensorDType.UInt8,
            "I8" => SafeTensorDType.Int8,
            "I16" => SafeTensorDType.Int16,
            "I32" => SafeTensorDType.Int32,
            "I64" => SafeTensorDType.Int64,
            "F16" => SafeTensorDType.Float16,
            "F32" => SafeTensorDType.Float32,
            "F64" => SafeTensorDType.Float64,
            "BF16" => SafeTensorDType.BFloat16,
            _ => SafeTensorDType.Float32
        };
    }

    private static long[] ParseShape(string arrayContent)
    {
        if (string.IsNullOrWhiteSpace(arrayContent))
        {
            return [];
        }

        var parts = arrayContent.Split(',');
        var shape = new long[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            shape[i] = long.Parse(parts[i].Trim());
        }

        return shape;
    }

    private static long[] ParseDataOffsets(string arrayContent)
    {
        var parts = arrayContent.Split(',');
        var offsets = new long[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            offsets[i] = long.Parse(parts[i].Trim());
        }

        return offsets;
    }

    private static List<string> SplitJsonEntries(string json)
    {
        var entries = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < json.Length; i++)
        {
            switch (json[i])
            {
                case '{' or '[':
                    depth++;
                    break;
                case '}' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    entries.Add(json[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        if (start < json.Length)
        {
            entries.Add(json[start..].Trim());
        }

        return entries;
    }

    #endregion
}
