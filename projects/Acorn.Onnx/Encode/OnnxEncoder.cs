using Acorn.Frame;
using Acorn.Onnx.Data;

namespace Acorn.Onnx.Encode;

/// <summary>
///     ONNX 模型编码器，将 C# 数据结构编码为 ONNX Protobuf 二进制格式。
/// </summary>
/// <remarks>
///     ONNX 使用 Protobuf 序列化，编码器按照 ONNX 规范将模型数据写入二进制缓冲区。
/// </remarks>
public static class OnnxEncoder
{
    /// <summary>
    ///     将 ONNX 模型数据编码写入缓冲区。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    /// <param name="model">要编码的 ONNX 模型数据。</param>
    /// <returns>已写入的字节数。</returns>
    public static int Encode(Span<byte> buffer, OnnxModelData model)
    {
        var writer = new ByteBufferWriter(buffer);
        EncodeCore(ref writer, model);
        writer.WrittenData.CopyTo(buffer);
        return writer.Position;
    }

    /// <summary>
    ///     将 ONNX 模型数据编码为字节数组。
    /// </summary>
    /// <param name="model">要编码的 ONNX 模型数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public static byte[] Encode(OnnxModelData model)
    {
        var writer = new ByteBufferWriter(1024 * 1024);
        EncodeCore(ref writer, model);
        return writer.ToArray();
    }

    private static void EncodeCore(ref ByteBufferWriter writer, OnnxModelData model)
    {
        if (model.IrVersion != 0)
        {
            WriteTag(ref writer, 1, 0);
            writer.WriteLeb128U64((ulong)model.IrVersion);
        }

        if (!string.IsNullOrEmpty(model.ProducerName))
        {
            WriteTag(ref writer, 2, 2);
            WriteString(ref writer, model.ProducerName);
        }

        if (!string.IsNullOrEmpty(model.ProducerVersion))
        {
            WriteTag(ref writer, 3, 2);
            WriteString(ref writer, model.ProducerVersion);
        }

        if (!string.IsNullOrEmpty(model.Domain))
        {
            WriteTag(ref writer, 4, 2);
            WriteString(ref writer, model.Domain);
        }

        if (model.ModelVersion != 0)
        {
            WriteTag(ref writer, 5, 0);
            writer.WriteLeb128U64((ulong)model.ModelVersion);
        }

        if (!string.IsNullOrEmpty(model.DocString))
        {
            WriteTag(ref writer, 6, 2);
            WriteString(ref writer, model.DocString);
        }

        if (model.Graph != null)
        {
            WriteTag(ref writer, 7, 2);
            WriteLengthDelimited(ref writer, EncodeGraph(model.Graph));
        }

        foreach (var opset in model.OpsetImport)
        {
            WriteTag(ref writer, 8, 2);
            WriteLengthDelimited(ref writer, EncodeOpsetImport(opset));
        }
    }

    private static byte[] EncodeGraph(OnnxGraph graph)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var node in graph.Node)
        {
            w.WriteU8((1 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeNode(node));
        }

        if (!string.IsNullOrEmpty(graph.Name))
        {
            w.WriteU8((2 << 3) | 2);
            WriteString(ref w, graph.Name);
        }

        foreach (var input in graph.Input)
        {
            w.WriteU8((3 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeValueInfo(input));
        }

        foreach (var output in graph.Output)
        {
            w.WriteU8((4 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeValueInfo(output));
        }

        foreach (var valueInfo in graph.ValueInfo)
        {
            w.WriteU8((5 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeValueInfo(valueInfo));
        }

        foreach (var initializer in graph.Initialization)
        {
            w.WriteU8((10 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeTensor(initializer));
        }

        if (!string.IsNullOrEmpty(graph.DocString))
        {
            w.WriteU8((12 << 3) | 2);
            WriteString(ref w, graph.DocString);
        }

        return w.ToArray();
    }

    private static byte[] EncodeNode(OnnxNode node)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var input in node.Input)
        {
            w.WriteU8((1 << 3) | 2);
            WriteString(ref w, input);
        }

        foreach (var output in node.Output)
        {
            w.WriteU8((2 << 3) | 2);
            WriteString(ref w, output);
        }

        if (!string.IsNullOrEmpty(node.Name))
        {
            w.WriteU8((3 << 3) | 2);
            WriteString(ref w, node.Name);
        }

        if (!string.IsNullOrEmpty(node.OpType))
        {
            w.WriteU8((4 << 3) | 2);
            WriteString(ref w, node.OpType);
        }

        if (!string.IsNullOrEmpty(node.Domain))
        {
            w.WriteU8((7 << 3) | 2);
            WriteString(ref w, node.Domain);
        }

        foreach (var attribute in node.Attribute)
        {
            w.WriteU8((8 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeAttribute(attribute));
        }

        if (!string.IsNullOrEmpty(node.DocString))
        {
            w.WriteU8((10 << 3) | 2);
            WriteString(ref w, node.DocString);
        }

        return w.ToArray();
    }

    private static byte[] EncodeValueInfo(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(valueInfo.Name))
        {
            w.WriteU8((1 << 3) | 2);
            WriteString(ref w, valueInfo.Name);
        }

        if (valueInfo.DataType != OnnxDataType.Undefined)
        {
            w.WriteU8((2 << 3) | 2);
            var typeBytes = EncodeTypeWithShape(valueInfo);
            WriteLengthDelimited(ref w, typeBytes);
        }

        return w.ToArray();
    }

    private static byte[] EncodeTypeWithShape(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.WriteU8((1 << 3) | 2);
        var tensorTypeBytes = EncodeTensorType(valueInfo);
        WriteLengthDelimited(ref w, tensorTypeBytes);

        return w.ToArray();
    }

    private static byte[] EncodeTensorType(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.WriteU8((1 << 3) | 0);
        w.WriteLeb128U64((ulong)valueInfo.DataType);

        if (valueInfo.Shape.Count > 0)
        {
            w.WriteU8((2 << 3) | 2);
            var shapeBytes = EncodeShape(valueInfo);
            WriteLengthDelimited(ref w, shapeBytes);
        }

        return w.ToArray();
    }

    private static byte[] EncodeShape(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var dim in valueInfo.Shape)
        {
            w.WriteU8((1 << 3) | 2);
            var dimBytes = EncodeDim(dim);
            WriteLengthDelimited(ref w, dimBytes);
        }

        return w.ToArray();
    }

    private static byte[] EncodeDim(long dim)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.WriteU8((1 << 3) | 0);
        w.WriteLeb128U64((ulong)dim);

        return w.ToArray();
    }

    private static byte[] EncodeTensor(OnnxTensor tensor)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var dim in tensor.Dims)
        {
            w.WriteU8((1 << 3) | 0);
            w.WriteLeb128U64((ulong)dim);
        }

        if (tensor.DataType != OnnxDataType.Undefined)
        {
            w.WriteU8((2 << 3) | 0);
            w.WriteLeb128U64((ulong)tensor.DataType);
        }

        if (tensor.RawData != null)
        {
            w.WriteU8((3 << 3) | 2);
            w.WriteLeb128U32((uint)tensor.RawData.Length);
            w.Write(tensor.RawData);
        }

        if (!string.IsNullOrEmpty(tensor.Name))
        {
            w.WriteU8((4 << 3) | 2);
            WriteString(ref w, tensor.Name);
        }

        foreach (var f in tensor.FloatData)
        {
            w.WriteU8((5 << 3) | 5);
            w.WriteF32LE(f);
        }

        foreach (var i in tensor.Int32Data)
        {
            w.WriteU8((6 << 3) | 5);
            w.WriteI32LE(i);
        }

        foreach (var i in tensor.Int64Data)
        {
            w.WriteU8((7 << 3) | 0);
            w.WriteLeb128U64((ulong)i);
        }

        foreach (var d in tensor.DoubleData)
        {
            w.WriteU8((9 << 3) | 1);
            w.WriteF64LE(d);
        }

        return w.ToArray();
    }

    private static byte[] EncodeAttribute(OnnxAttribute attribute)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(attribute.Name))
        {
            w.WriteU8((1 << 3) | 2);
            WriteString(ref w, attribute.Name);
        }

        if (attribute.Type != OnnxAttributeType.Undefined)
        {
            w.WriteLeb128U64((20 << 3) | 0);
            w.WriteLeb128U64((ulong)attribute.Type);
        }

        if (attribute.FloatValue != 0)
        {
            w.WriteU8((2 << 3) | 5);
            w.WriteF32LE(attribute.FloatValue);
        }

        if (attribute.IntValue != 0)
        {
            w.WriteU8((3 << 3) | 0);
            w.WriteLeb128U32((uint)attribute.IntValue);
        }

        if (!string.IsNullOrEmpty(attribute.StringValue))
        {
            w.WriteU8((4 << 3) | 2);
            WriteString(ref w, attribute.StringValue);
        }

        if (attribute.TensorValue != null)
        {
            w.WriteU8((5 << 3) | 2);
            WriteLengthDelimited(ref w, EncodeTensor(attribute.TensorValue));
        }

        foreach (var f in attribute.Floats)
        {
            w.WriteU8((7 << 3) | 5);
            w.WriteF32LE(f);
        }

        foreach (var i in attribute.Ints)
        {
            w.WriteU8((8 << 3) | 0);
            w.WriteLeb128U32((uint)i);
        }

        foreach (var s in attribute.Strings)
        {
            w.WriteU8((9 << 3) | 2);
            WriteString(ref w, s);
        }

        return w.ToArray();
    }

    private static byte[] EncodeOpsetImport(OnnxOperatorSetId opset)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(opset.Domain))
        {
            w.WriteU8((1 << 3) | 2);
            WriteString(ref w, opset.Domain);
        }

        if (opset.Version != 0)
        {
            w.WriteU8((2 << 3) | 0);
            w.WriteLeb128U64((ulong)opset.Version);
        }

        return w.ToArray();
    }

    #region 辅助方法

    private static void WriteTag(ref ByteBufferWriter writer, int fieldNumber, int wireType)
    {
        writer.WriteLeb128U64((ulong)((fieldNumber << 3) | wireType));
    }

    private static void WriteString(ref ByteBufferWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.WriteLeb128U32((uint)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteLengthDelimited(ref ByteBufferWriter writer, byte[] data)
    {
        writer.WriteLeb128U32((uint)data.Length);
        writer.Write(data);
    }

    #endregion
}
