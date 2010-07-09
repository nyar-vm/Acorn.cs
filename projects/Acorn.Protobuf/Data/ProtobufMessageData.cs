namespace Acorn.Protobuf.Data;

/// <summary>
///     Protobuf 消息数据结构。
/// </summary>
public class ProtobufMessageData
{
    /// <summary>
    ///     获取或设置消息名称。
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    ///     获取或设置消息字段。
    /// </summary>
    public List<ProtobufFieldData> Fields { get; set; } = new();
    
    /// <summary>
    ///     获取或设置嵌套消息。
    /// </summary>
    public List<ProtobufMessageData> NestedMessages { get; set; } = new();
}

/// <summary>
///     Protobuf 字段数据结构。
/// </summary>
public class ProtobufFieldData
{
    /// <summary>
    ///     获取或设置字段号。
    /// </summary>
    public int FieldNumber { get; set; }
    
    /// <summary>
    ///     获取或设置字段类型。
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    ///     获取或设置字段名称。
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    ///     获取或设置是否为重复字段。
    /// </summary>
    public bool IsRepeated { get; set; }
    
    /// <summary>
    ///     获取或设置是否为可选字段。
    /// </summary>
    public bool IsOptional { get; set; }
    
    /// <summary>
    ///     获取或设置是否为必填字段。
    /// </summary>
    public bool IsRequired { get; set; }
}