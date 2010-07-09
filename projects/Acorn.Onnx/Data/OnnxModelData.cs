namespace Acorn.Onnx.Data;

/// <summary>
///     ONNX 数据类型枚举，对应 onnx.TensorProto.DataType。
/// </summary>
public enum OnnxDataType
{
    Undefined = 0,
    Float = 1,
    UInt8 = 2,
    Int8 = 3,
    UInt16 = 4,
    Int16 = 5,
    Int32 = 6,
    Int64 = 7,
    String = 8,
    Bool = 9,
    Float16 = 10,
    Double = 11,
    UInt32 = 12,
    UInt64 = 13,
    Complex64 = 14,
    Complex128 = 15,
    BFloat16 = 16,
    Float8E4M3FN = 17,
    Float8E4M3FNUZ = 18,
    Float8E5M2 = 19,
    Float8E5M2FNUZ = 20
}

/// <summary>
///     ONNX 张量数据，对应 onnx.TensorProto。
/// </summary>
public sealed class OnnxTensor
{
    public string Name { get; init; } = string.Empty;
    public OnnxDataType DataType { get; init; }
    public IReadOnlyList<long> Dims { get; init; } = [];
    public byte[]? RawData { get; init; }
    public IReadOnlyList<float> FloatData { get; init; } = [];
    public IReadOnlyList<int> Int32Data { get; init; } = [];
    public IReadOnlyList<long> Int64Data { get; init; } = [];
    public IReadOnlyList<double> DoubleData { get; init; } = [];
}

/// <summary>
///     ONNX 节点属性值。
/// </summary>
public sealed class OnnxAttribute
{
    public string Name { get; init; } = string.Empty;
    public OnnxAttributeType Type { get; init; }
    public float FloatValue { get; init; }
    public int IntValue { get; init; }
    public string StringValue { get; init; } = string.Empty;
    public OnnxTensor? TensorValue { get; init; }
    public IReadOnlyList<float> Floats { get; init; } = [];
    public IReadOnlyList<int> Ints { get; init; } = [];
    public IReadOnlyList<string> Strings { get; init; } = [];
}

/// <summary>
///     ONNX 属性类型。
/// </summary>
public enum OnnxAttributeType
{
    Undefined = 0,
    Float = 1,
    Int = 2,
    String = 3,
    Tensor = 4,
    Graph = 5,
    Floats = 6,
    Ints = 7,
    Strings = 8,
    Tensors = 9,
    Graphs = 10
}

/// <summary>
///     ONNX 计算节点，对应 onnx.NodeProto。
/// </summary>
public sealed class OnnxNode
{
    public string Name { get; init; } = string.Empty;
    public string OpType { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string DocString { get; init; } = string.Empty;
    public IReadOnlyList<string> Input { get; init; } = [];
    public IReadOnlyList<string> Output { get; init; } = [];
    public IReadOnlyList<OnnxAttribute> Attribute { get; init; } = [];
}

/// <summary>
///     ONNX 值信息，对应 onnx.ValueInfoProto。
/// </summary>
public sealed class OnnxValueInfo
{
    public string Name { get; init; } = string.Empty;
    public OnnxDataType DataType { get; init; }
    public IReadOnlyList<long> Shape { get; init; } = [];
}

/// <summary>
///     ONNX 计算图，对应 onnx.GraphProto。
/// </summary>
public sealed class OnnxGraph
{
    public string Name { get; init; } = string.Empty;
    public string DocString { get; init; } = string.Empty;
    public IReadOnlyList<OnnxNode> Node { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> Input { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> Output { get; init; } = [];
    public IReadOnlyList<OnnxValueInfo> ValueInfo { get; init; } = [];
    public IReadOnlyList<OnnxTensor> Initialization { get; init; } = [];
}

/// <summary>
///     ONNX 算子集标识，对应 onnx.OperatorSetIdProto。
/// </summary>
public sealed class OnnxOperatorSetId
{
    public string Domain { get; init; } = string.Empty;
    public long Version { get; init; }
}

/// <summary>
///     ONNX 模型数据，对应 onnx.ModelProto。
/// </summary>
public sealed class OnnxModelData
{
    public long IrVersion { get; init; }
    public string ProducerName { get; init; } = string.Empty;
    public string ProducerVersion { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public long ModelVersion { get; init; }
    public string DocString { get; init; } = string.Empty;
    public OnnxGraph? Graph { get; init; }
    public IReadOnlyList<OnnxOperatorSetId> OpsetImport { get; init; } = [];
    public IReadOnlyList<string> CustomMetadata { get; init; } = [];
}
