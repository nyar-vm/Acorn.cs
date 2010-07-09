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
        EncodeCore(writer, model);
        return writer.Position;
    }

    /// <summary>
    ///     将 ONNX 模型数据编码为字节数组。
    /// </summary>
    /// <param name="model">要编码的 ONNX 模型数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public static byte[] Encode(OnnxModelData model)
    {
        var temp = new byte[1024 * 1024];
        var written = Encode(temp, model);
        return temp[..written].ToArray();
    }

    private static void EncodeCore(ByteBufferWriter writer, OnnxModelData model)
    {
        if (model.IrVersion != 0)
        {
            WriteTag(writer, 1, 0);
            writer.WriteLeb128U64((ulong)model.IrVersion);
        }

        if (!string.IsNullOrEmpty(model.ProducerName))
        {
            WriteTag(writer, 2, 2);
            WriteString(writer, model.ProducerName);
        }

        if (!string.IsNullOrEmpty(model.ProducerVersion))
        {
            WriteTag(writer, 3, 2);
            WriteString(writer, model.ProducerVersion);
        }

        if (!string.IsNullOrEmpty(model.Domain))
        {
            WriteTag(writer, 4, 2);
            WriteString(writer, model.Domain);
        }

        if (model.ModelVersion != 0)
        {
            WriteTag(writer, 5, 0);
            writer.WriteLeb128U64((ulong)model.ModelVersion);
        }

        if (!string.IsNullOrEmpty(model.DocString))
        {
            WriteTag(writer, 6, 2);
            WriteString(writer, model.DocString);
        }

        if (model.Graph != null)
        {
            WriteTag(writer, 7, 2);
            WriteLengthDelimited(writer, EncodeGraph(model.Graph));
        }

        foreach (var opset in model.OpsetImport)
        {
            WriteTag(writer, 8, 2);
            WriteLengthDelimited(writer, EncodeOpsetImport(opset));
        }
    }

    private static byte[] EncodeGraph(OnnxGraph graph)
    {
        return BuildBytes(w =>
        {
            foreach (var node in graph.Node)
            {
                w.WriteU8((1 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeNode(node));
            }

            if (!string.IsNullOrEmpty(graph.Name))
            {
                w.WriteU8((2 << 3) | 2);
                WriteStringTo(w, graph.Name);
            }

            foreach (var input in graph.Input)
            {
                w.WriteU8((3 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeValueInfo(input));
            }

            foreach (var output in graph.Output)
            {
                w.WriteU8((4 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeValueInfo(output));
            }

            foreach (var valueInfo in graph.ValueInfo)
            {
                w.WriteU8((5 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeValueInfo(valueInfo));
            }

            foreach (var initializer in graph.Initialization)
            {
                w.WriteU8((10 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeTensor(initializer));
            }

            if (!string.IsNullOrEmpty(graph.DocString))
            {
                w.WriteU8((12 << 3) | 2);
                WriteStringTo(w, graph.DocString);
            }
        });
    }

    private static byte[] EncodeNode(OnnxNode node)
    {
        return BuildBytes(w =>
        {
            foreach (var input in node.Input)
            {
                w.WriteU8((1 << 3) | 2);
                WriteStringTo(w, input);
            }

            foreach (var output in node.Output)
            {
                w.WriteU8((2 << 3) | 2);
                WriteStringTo(w, output);
            }

            if (!string.IsNullOrEmpty(node.Name))
            {
                w.WriteU8((3 << 3) | 2);
                WriteStringTo(w, node.Name);
            }

            if (!string.IsNullOrEmpty(node.OpType))
            {
                w.WriteU8((4 << 3) | 2);
                WriteStringTo(w, node.OpType);
            }

            if (!string.IsNullOrEmpty(node.Domain))
            {
                w.WriteU8((7 << 3) | 2);
                WriteStringTo(w, node.Domain);
            }

            foreach (var attribute in node.Attribute)
            {
                w.WriteU8((8 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeAttribute(attribute));
            }

            if (!string.IsNullOrEmpty(node.DocString))
            {
                w.WriteU8((10 << 3) | 2);
                WriteStringTo(w, node.DocString);
            }
        });
    }

    private static byte[] EncodeValueInfo(OnnxValueInfo valueInfo)
    {
        return BuildBytes(w =>
        {
            if (!string.IsNullOrEmpty(valueInfo.Name))
            {
                w.WriteU8((1 << 3) | 2);
                WriteStringTo(w, valueInfo.Name);
            }

            if (valueInfo.DataType != OnnxDataType.Undefined)
            {
                w.WriteU8((2 << 3) | 2);
                WriteLengthDelimitedTo(w, BuildBytes(tw =>
                {
                    tw.WriteU8((1 << 3) | 2);
                    WriteLengthDelimitedTo(tw, BuildBytes(ttw =>
                    {
                        ttw.WriteU8((1 << 3) | 0);
                        ttw.WriteLeb128U64((ulong)valueInfo.DataType);

                        if (valueInfo.Shape.Count > 0)
                        {
                            ttw.WriteU8((2 << 3) | 2);
                            WriteLengthDelimitedTo(ttw, BuildBytes(sw =>
                            {
                                foreach (var dim in valueInfo.Shape)
                                {
                                    sw.WriteU8((1 << 3) | 2);
                                    WriteLengthDelimitedTo(sw, BuildBytes(dw =>
                                    {
                                        dw.WriteU8((1 << 3) | 0);
                                        dw.WriteLeb128U64((ulong)dim);
                                    }));
                                }
                            }));
                        }
                    }));
                }));
            }
        });
    }

    private static byte[] EncodeTensor(OnnxTensor tensor)
    {
        return BuildBytes(w =>
        {
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
                WriteStringTo(w, tensor.Name);
            }

            foreach (var f in tensor.FloatData)
            {
                w.WriteU8((5 << 3) | 5);
                w.WriteF32LE(f);
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

            foreach (var i in tensor.Int32Data)
            {
                w.WriteU8((6 << 3) | 5);
                w.WriteI32LE(i);
            }
        });
    }

    private static byte[] EncodeAttribute(OnnxAttribute attribute)
    {
        return BuildBytes(w =>
        {
            if (!string.IsNullOrEmpty(attribute.Name))
            {
                w.WriteU8((1 << 3) | 2);
                WriteStringTo(w, attribute.Name);
            }

            if (attribute.Type != OnnxAttributeType.Undefined)
            {
                w.WriteU8((20 << 3) | 0);
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
                WriteStringTo(w, attribute.StringValue);
            }

            if (attribute.TensorValue != null)
            {
                w.WriteU8((5 << 3) | 2);
                WriteLengthDelimitedTo(w, EncodeTensor(attribute.TensorValue));
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
                WriteStringTo(w, s);
            }
        });
    }

    private static byte[] EncodeOpsetImport(OnnxOperatorSetId opset)
    {
        return BuildBytes(w =>
        {
            if (!string.IsNullOrEmpty(opset.Domain))
            {
                w.WriteU8((1 << 3) | 2);
                WriteStringTo(w, opset.Domain);
            }

            if (opset.Version != 0)
            {
                w.WriteU8((2 << 3) | 0);
                w.WriteLeb128U64((ulong)opset.Version);
            }
        });
    }

    #region 辅助方法

    private static void WriteTag(ByteBufferWriter writer, int fieldNumber, int wireType)
    {
        writer.WriteLeb128U64((ulong)((fieldNumber << 3) | wireType));
    }

    private static void WriteString(ByteBufferWriter writer, string value)
    {
        WriteStringTo(writer, value);
    }

    private static void WriteStringTo(ByteBufferWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.WriteLeb128U32((uint)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteLengthDelimited(ByteBufferWriter writer, byte[] data)
    {
        WriteLengthDelimitedTo(writer, data);
    }

    private static void WriteLengthDelimitedTo(ByteBufferWriter writer, byte[] data)
    {
        writer.WriteLeb128U32((uint)data.Length);
        writer.Write(data);
    }

    private static byte[] BuildBytes(Action<ByteBufferWriter> writeContent)
    {
        var temp = new byte[1024 * 1024];
        var tempWriter = new ByteBufferWriter(temp);
        writeContent(tempWriter);
        return temp[..tempWriter.Position].ToArray();
    }

    #endregion
}
