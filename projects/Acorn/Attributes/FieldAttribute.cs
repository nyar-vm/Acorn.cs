using Acorn;
using Acorn.Codec;

namespace Acorn.Attributes;

/// <summary>
///     标记二进制结构体中的字段，指定序列化顺序和编码方式。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class FieldAttribute : Attribute
{
    /// <summary>
    ///     序列化顺序。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     固定长度（用于数组/字符串），-1 表示变长。
    /// </summary>
    public int Length { get; set; } = -1;

    /// <summary>
    ///     变长数组的长度来源字段名。
    /// </summary>
    public string LengthField { get; set; } = "";

    /// <summary>
    ///     条件存在：指向 bool 属性/方法名。
    /// </summary>
    public string ConditionalOn { get; set; } = "";

    /// <summary>
    ///     局部覆盖字节序。
    /// </summary>
    public Endianness Endianness { get; set; }

    /// <summary>
    ///     字符串编码名称，默认 utf-8。
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    ///     标记字段为可选字段，当缓冲区数据不足时跳过该字段。
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    ///     自定义编解码器类型，实现 ICodec&lt;T&gt; 接口。
    /// </summary>
    public Type? Codec { get; set; }
}
