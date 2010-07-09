using System.Text;
using Acorn.Frame;
using Acorn.SafeTensors.Data;

namespace Acorn.SafeTensors.Encode;

/// <summary>
///     SafeTensors 格式编码器，将 C# 数据结构编码为 HuggingFace SafeTensors 格式。
/// </summary>
/// <remarks>
///     SafeTensors 是 HuggingFace 定义的张量存储格式，使用小端序存储数值，
///     文件头为 JSON 格式，包含张量名称、数据类型、形状和偏移量等元信息。
///     编码器生成符合 SafeTensors 规范的二进制数据。
/// </remarks>
public sealed class SafeTensorsEncoder
{
    /// <summary>
    ///     将 SafeTensors 文件数据编码为二进制格式。
    /// </summary>
    /// <param name="data">SafeTensors 文件数据。</param>
    /// <returns>SafeTensors 二进制数据。</returns>
    public byte[] Encode(SafeTensorsFileData data)
    {
        var headerJson = BuildHeaderJsonFromMeta(data.Tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        while (headerBytes.Length % 8 != 0)
        {
            headerJson += " ";
            headerBytes = Encoding.UTF8.GetBytes(headerJson);
        }

        var totalSize = 8 + headerBytes.Length + (data.Data?.Length ?? 0);
        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteU64LE((ulong)headerBytes.Length);
        writer.Write(headerBytes);

        if (data.Data != null)
        {
            writer.Write(data.Data);
        }

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     将张量列表编码为 SafeTensors 二进制格式。
    /// </summary>
    /// <param name="tensors">张量数据列表。</param>
    /// <returns>SafeTensors 二进制数据。</returns>
    public byte[] EncodeTensors(IReadOnlyList<SafeTensorData> tensors)
    {
        var headerJson = BuildHeaderJson(tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        while (headerBytes.Length % 8 != 0)
        {
            headerJson += " ";
            headerBytes = Encoding.UTF8.GetBytes(headerJson);
        }

        var totalSize = 8 + headerBytes.Length + CalculateTensorDataSize(tensors);
        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteU64LE((ulong)headerBytes.Length);
        writer.Write(headerBytes);

        foreach (var tensor in tensors)
        {
            writer.Write(tensor.Data);
        }

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static string BuildHeaderJsonFromMeta(IReadOnlyDictionary<string, SafeTensorMeta> tensors)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        ulong offset = 0;
        var first = true;

        foreach (var (name, meta) in tensors)
        {
            if (!first)
            {
                sb.Append(',');
            }

            first = false;

            var dtype = FormatDType(meta.DType);
            var shape = FormatShape(meta.Shape);
            var dataEnd = offset + (ulong)meta.DataLength;

            sb.Append($"\"{name}\":{{\"dtype\":\"{dtype}\",\"shape\":{shape},\"data_offsets\":[{offset},{dataEnd}]}}");

            offset = dataEnd;
        }

        sb.Append(",\"__metadata__\":{}");
        sb.Append('}');

        return sb.ToString();
    }

    private static string BuildHeaderJson(IReadOnlyList<SafeTensorData> tensors)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        ulong offset = 0;

        for (var i = 0; i < tensors.Count; i++)
        {
            var tensor = tensors[i];

            if (i > 0)
            {
                sb.Append(',');
            }

            var dtype = FormatDType(tensor.DType);
            var shape = FormatShape(tensor.Shape);
            var dataEnd = offset + (ulong)tensor.Data.Length;

            sb.Append($"\"{tensor.Name}\":{{\"dtype\":\"{dtype}\",\"shape\":{shape},\"data_offsets\":[{offset},{dataEnd}]}}");

            offset = dataEnd;
        }

        sb.Append(",\"__metadata__\":{}");
        sb.Append('}');

        return sb.ToString();
    }

    private static string FormatDType(SafeTensorDType dtype)
    {
        return dtype switch
        {
            SafeTensorDType.Bool => "BOOL",
            SafeTensorDType.UInt8 => "U8",
            SafeTensorDType.Int8 => "I8",
            SafeTensorDType.Int16 => "I16",
            SafeTensorDType.Int32 => "I32",
            SafeTensorDType.Int64 => "I64",
            SafeTensorDType.Float16 => "F16",
            SafeTensorDType.Float32 => "F32",
            SafeTensorDType.Float64 => "F64",
            SafeTensorDType.BFloat16 => "BF16",
            _ => "F32"
        };
    }

    private static string FormatShape(IReadOnlyList<long> shape)
    {
        if (shape.Count == 0)
        {
            return "[]";
        }

        return $"[{string.Join(",", shape)}]";
    }

    private static int CalculateTensorDataSize(IReadOnlyList<SafeTensorData> tensors)
    {
        var size = 0;

        foreach (var tensor in tensors)
        {
            size += tensor.Data.Length;
        }

        return size;
    }

    #endregion
}
