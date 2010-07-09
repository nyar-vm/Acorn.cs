namespace Acorn.Protobuf.Data;

/// <summary>
///     Protobuf 相关常量定义。
/// </summary>
public static class ProtobufConstants
{
    /// <summary>
    ///     Protobuf wire type 定义。
    /// </summary>
    public enum WireType
    {
        /// <summary>
        ///     可变长度整数。
        /// </summary>
        Varint = 0,
        
        /// <summary>
        ///     64 位值。
        /// </summary>
        Fixed64 = 1,
        
        /// <summary>
        ///     长度前缀的字符串/消息。
        /// </summary>
        LengthDelimited = 2,
        
        /// <summary>
        ///     32 位值。
        /// </summary>
        Fixed32 = 5
    }
    
    /// <summary>
    ///     最大字段号。
    /// </summary>
    public const int MaxFieldNumber = 19000;
    
    /// <summary>
    ///     字段号掩码。
    /// </summary>
    public const int FieldNumberMask = 0x07;
    
    /// <summary>
    ///     Wire type 掩码。
    /// </summary>
    public const int WireTypeMask = 0x7F;
}