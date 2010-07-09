namespace Acorn.SafeTensors.Data;

/// <summary>
///     SafeTensors 数据类型，对应 HuggingFace SafeTensors 规范中的 dtype 字段。
/// </summary>
public enum SafeTensorDType
{
    Bool,
    UInt8,
    Int8,
    Int16,
    Int32,
    Int64,
    Float16,
    Float32,
    Float64,
    BFloat16
}

/// <summary>
///     SafeTensors 张量元数据，对应 JSON 头中的每个张量条目。
/// </summary>
public sealed class SafeTensorMeta
{
    /// <summary>
    ///     张量数据类型。
    /// </summary>
    public SafeTensorDType DType { get; init; }

    /// <summary>
    ///     张量形状。
    /// </summary>
    public IReadOnlyList<long> Shape { get; init; } = [];

    /// <summary>
    ///     数据在文件中的偏移量（相对于数据区起始位置）。
    /// </summary>
    public long DataOffset { get; init; }

    /// <summary>
    ///     数据长度（字节数）。
    /// </summary>
    public long DataLength { get; init; }
}

/// <summary>
///     SafeTensors 文件数据，表示一个完整的 SafeTensors 文件。
/// </summary>
/// <remarks>
///     SafeTensors 是 HuggingFace 定义的模型权重存储格式，结构为：
///     8 字节头长度（小端序 uint64）+ JSON 头 + 二进制张量数据。
///     JSON 头中每个张量条目包含 dtype、shape、data_offsets 字段。
///     __metadata__ 条目存储自定义元数据。
/// </remarks>
public sealed class SafeTensorsFileData
{
    /// <summary>
    ///     张量元数据字典（键为张量名称）。
    /// </summary>
    public IReadOnlyDictionary<string, SafeTensorMeta> Tensors { get; init; } = new Dictionary<string, SafeTensorMeta>();

    /// <summary>
    ///     自定义元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    ///     原始二进制数据区。
    /// </summary>
    public byte[]? Data { get; init; }
}

/// <summary>
///     SafeTensors 张量数据（含实际值）。
/// </summary>
public sealed class SafeTensorData
{
    /// <summary>
    ///     张量名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     数据类型。
    /// </summary>
    public SafeTensorDType DType { get; init; }

    /// <summary>
    ///     张量形状。
    /// </summary>
    public IReadOnlyList<long> Shape { get; init; } = [];

    /// <summary>
    ///     原始字节数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}
