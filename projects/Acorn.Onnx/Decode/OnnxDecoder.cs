using Acorn.Frame;
using Acorn.Onnx.Data;

namespace Acorn.Onnx.Decode;

/// <summary>
///     ONNX 模型解码器，将 ONNX Protobuf 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     ONNX 使用 Protobuf 序列化，文件头可能包含魔数标记。
///     本解码器解析完整的 ONNX ModelProto 结构。
/// </remarks>
public sealed class OnnxDecoder
{
    /// <summary>
    ///     从 ONNX 二进制数据解码模型。
    /// </summary>
    /// <param name="data">ONNX 二进制数据。</param>
    /// <returns>解码后的模型数据。</returns>
    public OnnxModelData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        return DecodeModel(buffer);
    }

    private OnnxModelData DecodeModel(ByteBuffer buffer)
    {
        long irVersion = 0;
        var producerName = string.Empty;
        var producerVersion = string.Empty;
        var domain = string.Empty;
        long modelVersion = 0;
        var docString = string.Empty;
        OnnxGraph? graph = null;
        var opsetImports = new List<OnnxOperatorSetId>();

        while (!buffer.IsEnd)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: irVersion = (long)buffer.ReadLeb128U64(); break;
                case 2: producerName = ReadString(buffer); break;
                case 3: producerVersion = ReadString(buffer); break;
                case 4: domain = ReadString(buffer); break;
                case 5: modelVersion = (long)buffer.ReadLeb128U64(); break;
                case 6: docString = ReadString(buffer); break;
                case 7: graph = DecodeGraph(buffer); break;
                case 8: opsetImports.Add(DecodeOpsetImport(buffer)); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxModelData
        {
            IrVersion = irVersion,
            ProducerName = producerName,
            ProducerVersion = producerVersion,
            Domain = domain,
            ModelVersion = modelVersion,
            DocString = docString,
            Graph = graph,
            OpsetImport = opsetImports
        };
    }

    private OnnxGraph DecodeGraph(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var name = string.Empty;
        var docString = string.Empty;
        var nodes = new List<OnnxNode>();
        var inputs = new List<OnnxValueInfo>();
        var outputs = new List<OnnxValueInfo>();
        var valueInfos = new List<OnnxValueInfo>();
        var initializers = new List<OnnxTensor>();

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: nodes.Add(DecodeNode(buffer)); break;
                case 2: name = ReadString(buffer); break;
                case 3: inputs.Add(DecodeValueInfo(buffer)); break;
                case 4: outputs.Add(DecodeValueInfo(buffer)); break;
                case 5: valueInfos.Add(DecodeValueInfo(buffer)); break;
                case 10: initializers.Add(DecodeTensor(buffer)); break;
                case 12: docString = ReadString(buffer); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxGraph
        {
            Name = name,
            DocString = docString,
            Node = nodes,
            Input = inputs,
            Output = outputs,
            ValueInfo = valueInfos,
            Initialization = initializers
        };
    }

    private OnnxNode DecodeNode(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var name = string.Empty;
        var opType = string.Empty;
        var domain = string.Empty;
        var docString = string.Empty;
        var input = new List<string>();
        var output = new List<string>();
        var attributes = new List<OnnxAttribute>();

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: input.Add(ReadString(buffer)); break;
                case 2: output.Add(ReadString(buffer)); break;
                case 3: name = ReadString(buffer); break;
                case 4: opType = ReadString(buffer); break;
                case 7: domain = ReadString(buffer); break;
                case 8: attributes.Add(DecodeAttribute(buffer)); break;
                case 10: docString = ReadString(buffer); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxNode
        {
            Name = name,
            OpType = opType,
            Domain = domain,
            DocString = docString,
            Input = input,
            Output = output,
            Attribute = attributes
        };
    }

    private OnnxValueInfo DecodeValueInfo(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var name = string.Empty;
        var dataType = OnnxDataType.Undefined;
        var shape = new List<long>();

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: name = ReadString(buffer); break;
                case 2:
                    var typeLength = (int)buffer.ReadLeb128U32();
                    var typeEnd = buffer.Position + typeLength;

                    while (buffer.Position < typeEnd)
                    {
                        var (tf, tw) = ReadTag(buffer);

                        switch (tf)
                        {
                            case 1:
                                var tensorTypeLen = (int)buffer.ReadLeb128U32();
                                var tensorTypeEnd = buffer.Position + tensorTypeLen;

                                while (buffer.Position < tensorTypeEnd)
                                {
                                    var (ttf, ttw) = ReadTag(buffer);

                                    switch (ttf)
                                    {
                                        case 1: dataType = (OnnxDataType)(long)buffer.ReadLeb128U64(); break;
                                        case 2:
                                            var shapeLen = (int)buffer.ReadLeb128U32();
                                            var shapeEnd = buffer.Position + shapeLen;

                                            while (buffer.Position < shapeEnd)
                                            {
                                                var (sf, sw) = ReadTag(buffer);

                                                if (sf == 1)
                                                {
                                                    var dimLen = (int)buffer.ReadLeb128U32();
                                                    var dimEnd = buffer.Position + dimLen;

                                                    while (buffer.Position < dimEnd)
                                                    {
                                                        var (df, dw) = ReadTag(buffer);

                                                        if (df == 1)
                                                        {
                                                            shape.Add((long)buffer.ReadLeb128U64());
                                                        }
                                                        else
                                                        {
                                                            SkipField(buffer, dw);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    SkipField(buffer, sw);
                                                }
                                            }

                                            break;
                                        default: SkipField(buffer, ttw); break;
                                    }
                                }

                                break;
                            default: SkipField(buffer, tw); break;
                        }
                    }

                    break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxValueInfo
        {
            Name = name,
            DataType = dataType,
            Shape = shape
        };
    }

    private OnnxTensor DecodeTensor(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var name = string.Empty;
        var dataType = OnnxDataType.Undefined;
        var dims = new List<long>();
        byte[]? rawData = null;
        var floatData = new List<float>();
        var int32Data = new List<int>();
        var int64Data = new List<long>();
        var doubleData = new List<double>();

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: dims.Add((long)buffer.ReadLeb128U64()); break;
                case 2: dataType = (OnnxDataType)(long)buffer.ReadLeb128U64(); break;
                case 3:
                    if (wireType == 2)
                    {
                        var rawLen = (int)buffer.ReadLeb128U32();
                        rawData = buffer.ReadBytes(rawLen).ToArray();
                    }
                    break;
                case 4: name = ReadString(buffer); break;
                case 5: floatData.Add(buffer.ReadF32LE()); break;
                case 7: int64Data.Add((long)buffer.ReadLeb128U64()); break;
                case 9: doubleData.Add(buffer.ReadF64LE()); break;
                case 6: int32Data.Add((int)buffer.ReadLeb128U32()); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxTensor
        {
            Name = name,
            DataType = dataType,
            Dims = dims,
            RawData = rawData,
            FloatData = floatData,
            Int32Data = int32Data,
            Int64Data = int64Data,
            DoubleData = doubleData
        };
    }

    private OnnxAttribute DecodeAttribute(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var name = string.Empty;
        var type = OnnxAttributeType.Undefined;
        float floatValue = 0;
        var intValue = 0;
        var stringValue = string.Empty;
        OnnxTensor? tensorValue = null;
        var floats = new List<float>();
        var ints = new List<int>();
        var strings = new List<string>();

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: name = ReadString(buffer); break;
                case 20: type = (OnnxAttributeType)buffer.ReadLeb128U64(); break;
                case 2: floatValue = buffer.ReadF32LE(); break;
                case 3: intValue = (int)buffer.ReadLeb128U32(); break;
                case 4: stringValue = ReadString(buffer); break;
                case 5: tensorValue = DecodeTensor(buffer); break;
                case 7: floats.Add(buffer.ReadF32LE()); break;
                case 8: ints.Add((int)buffer.ReadLeb128U32()); break;
                case 9: strings.Add(ReadString(buffer)); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxAttribute
        {
            Name = name,
            Type = type,
            FloatValue = floatValue,
            IntValue = intValue,
            StringValue = stringValue,
            TensorValue = tensorValue,
            Floats = floats,
            Ints = ints,
            Strings = strings
        };
    }

    private OnnxOperatorSetId DecodeOpsetImport(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        var endPosition = buffer.Position + length;

        var domain = string.Empty;
        long version = 0;

        while (buffer.Position < endPosition)
        {
            var (fieldNumber, wireType) = ReadTag(buffer);

            switch (fieldNumber)
            {
                case 1: domain = ReadString(buffer); break;
                case 2: version = (long)buffer.ReadLeb128U64(); break;
                default: SkipField(buffer, wireType); break;
            }
        }

        return new OnnxOperatorSetId
        {
            Domain = domain,
            Version = version
        };
    }

    #region 辅助方法

    private static (int FieldNumber, int WireType) ReadTag(ByteBuffer buffer)
    {
        var tag = buffer.ReadLeb128U64();
        return ((int)(tag >> 3), (int)(tag & 0x7));
    }

    private static string ReadString(ByteBuffer buffer)
    {
        var length = (int)buffer.ReadLeb128U32();
        return buffer.ReadString(length);
    }

    private static void SkipField(ByteBuffer buffer, int wireType)
    {
        switch (wireType)
        {
            case 0: buffer.ReadLeb128U64(); break;
            case 1: buffer.Advance(8); break;
            case 2:
                var len = (int)buffer.ReadLeb128U32();
                buffer.Advance(len);
                break;
            case 5: buffer.Advance(4); break;
        }
    }

    #endregion
}
