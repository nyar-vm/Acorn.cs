using System.Text;
using Acorn.Frame;
using Acorn.Jvm.Data;

namespace Acorn.Jvm.Scanner;

/// <summary>
///     JVM ClassFile 二进制格式扫描器，基于 <see cref="SpanScanner" /> 提供对 .class 文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     JVM ClassFile 使用大端字节序（Big-Endian），与多数其他二进制格式不同。
///     扫描器通过逐段跳过的方式快速提取结构统计信息，不完整解析常量池。
/// </remarks>
public ref struct JvmScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="JvmScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 JVM ClassFile 字节数据。</param>
    public JvmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 JVM ClassFile 头部，提取版本与类元信息。
    /// </summary>
    public JvmScanHeader ScanHeader()
    {
        if (_scanner.Length < 10)
        {
            throw new InvalidDataException("JVM ClassFile 数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(JvmConstants.MagicBigEndian))
        {
            throw new InvalidDataException("JVM ClassFile 魔数不匹配，期望 0xCAFEBABE");
        }

        _scanner.ConsumeMagic(JvmConstants.MagicBigEndian);

        var minorVersion = _scanner.Buffer.ReadU16BE();
        var majorVersion = _scanner.Buffer.ReadU16BE();

        var header = new JvmScanHeader
        {
            MinorVersion = minorVersion,
            MajorVersion = majorVersion
        };

        var constantPoolCount = _scanner.Buffer.ReadU16BE();
        header.ConstantPoolCount = constantPoolCount;

        var className = string.Empty;

        for (var i = 1; i < constantPoolCount; i++)
        {
            if (_scanner.Buffer.Remaining < 1)
            {
                break;
            }

            var tag = _scanner.Buffer.ReadU8();

            switch ((JvmConstantKind)tag)
            {
                case JvmConstantKind.Utf8:
                    if (_scanner.Buffer.Remaining < 2)
                    {
                        break;
                    }

                    var length = _scanner.Buffer.ReadU16BE();

                    if (_scanner.Buffer.Remaining >= length)
                    {
                        _scanner.Buffer.Position += length;
                    }

                    break;

                case JvmConstantKind.Integer:
                case JvmConstantKind.Float:
                case JvmConstantKind.Fieldref:
                case JvmConstantKind.Methodref:
                case JvmConstantKind.InterfaceMethodref:
                case JvmConstantKind.NameAndType:
                case JvmConstantKind.InvokeDynamic:
                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.Long:
                case JvmConstantKind.Double:
                    if (_scanner.Buffer.Remaining >= 8)
                    {
                        _scanner.Buffer.Position += 8;
                    }

                    i++;
                    break;

                case JvmConstantKind.Class:
                case JvmConstantKind.String:
                case JvmConstantKind.MethodType:
                    if (_scanner.Buffer.Remaining >= 2)
                    {
                        _scanner.Buffer.Position += 2;
                    }

                    break;

                case JvmConstantKind.MethodHandle:
                    if (_scanner.Buffer.Remaining >= 3)
                    {
                        _scanner.Buffer.Position += 3;
                    }

                    break;

                default:
                    break;
            }
        }

        if (_scanner.Buffer.Remaining < 6)
        {
            return header;
        }

        header.AccessFlags = _scanner.Buffer.ReadU16BE();
        header.ThisClassIndex = _scanner.Buffer.ReadU16BE();
        header.SuperClassIndex = _scanner.Buffer.ReadU16BE();

        return header;
    }

    /// <summary>
    ///     扫描 JVM ClassFile，提取统计信息。
    /// </summary>
    public JvmStatistics ScanStatistics()
    {
        var stats = new JvmStatistics();

        if (_scanner.Length < 10)
        {
            throw new InvalidDataException("JVM ClassFile 数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(JvmConstants.MagicBigEndian))
        {
            throw new InvalidDataException("JVM ClassFile 魔数不匹配，期望 0xCAFEBABE");
        }

        _scanner.ConsumeMagic(JvmConstants.MagicBigEndian);

        stats.MinorVersion = _scanner.Buffer.ReadU16BE();
        stats.MajorVersion = _scanner.Buffer.ReadU16BE();

        var constantPoolCount = _scanner.Buffer.ReadU16BE();
        stats.ConstantPoolCount = constantPoolCount;

        for (var i = 1; i < constantPoolCount; i++)
        {
            if (_scanner.Buffer.Remaining < 1)
            {
                break;
            }

            var tag = _scanner.Buffer.ReadU8();

            switch ((JvmConstantKind)tag)
            {
                case JvmConstantKind.Utf8:
                    if (_scanner.Buffer.Remaining < 2)
                    {
                        break;
                    }

                    var utf8Length = _scanner.Buffer.ReadU16BE();
                    stats.Utf8ConstantCount++;

                    if (_scanner.Buffer.Remaining >= utf8Length)
                    {
                        _scanner.Buffer.Position += utf8Length;
                    }

                    break;

                case JvmConstantKind.Integer:
                    stats.IntegerConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.Float:
                    stats.FloatConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.Long:
                    stats.LongConstantCount++;

                    if (_scanner.Buffer.Remaining >= 8)
                    {
                        _scanner.Buffer.Position += 8;
                    }

                    i++;
                    break;

                case JvmConstantKind.Double:
                    stats.DoubleConstantCount++;

                    if (_scanner.Buffer.Remaining >= 8)
                    {
                        _scanner.Buffer.Position += 8;
                    }

                    i++;
                    break;

                case JvmConstantKind.Class:
                    stats.ClassConstantCount++;

                    if (_scanner.Buffer.Remaining >= 2)
                    {
                        _scanner.Buffer.Position += 2;
                    }

                    break;

                case JvmConstantKind.String:
                    stats.StringConstantCount++;

                    if (_scanner.Buffer.Remaining >= 2)
                    {
                        _scanner.Buffer.Position += 2;
                    }

                    break;

                case JvmConstantKind.Fieldref:
                    stats.FieldrefConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.Methodref:
                    stats.MethodrefConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.InterfaceMethodref:
                    stats.InterfaceMethodrefConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.NameAndType:
                    stats.NameAndTypeConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                case JvmConstantKind.MethodHandle:
                    stats.MethodHandleConstantCount++;

                    if (_scanner.Buffer.Remaining >= 3)
                    {
                        _scanner.Buffer.Position += 3;
                    }

                    break;

                case JvmConstantKind.MethodType:
                    stats.MethodTypeConstantCount++;

                    if (_scanner.Buffer.Remaining >= 2)
                    {
                        _scanner.Buffer.Position += 2;
                    }

                    break;

                case JvmConstantKind.InvokeDynamic:
                    stats.InvokeDynamicConstantCount++;

                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        _scanner.Buffer.Position += 4;
                    }

                    break;

                default:
                    break;
            }
        }

        if (_scanner.Buffer.Remaining < 8)
        {
            return stats;
        }

        stats.AccessFlags = _scanner.Buffer.ReadU16BE();
        _scanner.Buffer.Position += 4;

        var interfacesCount = _scanner.Buffer.ReadU16BE();
        stats.InterfaceCount = interfacesCount;

        if (_scanner.Buffer.Remaining >= interfacesCount * 2)
        {
            _scanner.Buffer.Position += interfacesCount * 2;
        }

        if (_scanner.Buffer.Remaining < 2)
        {
            return stats;
        }

        var fieldsCount = _scanner.Buffer.ReadU16BE();
        stats.FieldCount = fieldsCount;

        for (var i = 0; i < fieldsCount; i++)
        {
            if (_scanner.Buffer.Remaining < 8)
            {
                break;
            }

            _scanner.Buffer.Position += 6;

            if (_scanner.Buffer.Remaining < 2)
            {
                break;
            }

            var fieldAttrCount = _scanner.Buffer.ReadU16BE();

            for (var j = 0; j < fieldAttrCount; j++)
            {
                if (_scanner.Buffer.Remaining < 6)
                {
                    break;
                }

                _scanner.Buffer.Position += 2;
                var attrLength = _scanner.Buffer.ReadU32BE();

                if (_scanner.Buffer.Remaining >= (int)attrLength)
                {
                    _scanner.Buffer.Position += (int)attrLength;
                }
            }
        }

        if (_scanner.Buffer.Remaining < 2)
        {
            return stats;
        }

        var methodsCount = _scanner.Buffer.ReadU16BE();
        stats.MethodCount = methodsCount;

        for (var i = 0; i < methodsCount; i++)
        {
            if (_scanner.Buffer.Remaining < 8)
            {
                break;
            }

            _scanner.Buffer.Position += 6;

            if (_scanner.Buffer.Remaining < 2)
            {
                break;
            }

            var methodAttrCount = _scanner.Buffer.ReadU16BE();

            for (var j = 0; j < methodAttrCount; j++)
            {
                if (_scanner.Buffer.Remaining < 6)
                {
                    break;
                }

                _scanner.Buffer.Position += 2;
                var attrLength = _scanner.Buffer.ReadU32BE();

                stats.TotalMethodAttributeBytes += attrLength;

                if (_scanner.Buffer.Remaining >= (int)attrLength)
                {
                    _scanner.Buffer.Position += (int)attrLength;
                }
            }
        }

        if (_scanner.Buffer.Remaining < 2)
        {
            return stats;
        }

        var classAttrCount = _scanner.Buffer.ReadU16BE();
        stats.ClassAttributeCount = classAttrCount;

        for (var i = 0; i < classAttrCount; i++)
        {
            if (_scanner.Buffer.Remaining < 6)
            {
                break;
            }

            _scanner.Buffer.Position += 2;
            var attrLength = _scanner.Buffer.ReadU32BE();

            if (_scanner.Buffer.Remaining >= (int)attrLength)
            {
                _scanner.Buffer.Position += (int)attrLength;
            }
        }

        return stats;
    }

    /// <summary>
    ///     快速判断数据是否为有效的 JVM ClassFile。
    /// </summary>
    public bool IsJvmClass()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        if (!_scanner.MatchMagic(JvmConstants.MagicBigEndian))
        {
            return false;
        }

        try
        {
            _scanner.ConsumeMagic(JvmConstants.MagicBigEndian);

            if (_scanner.Buffer.Remaining < 4)
            {
                return false;
            }

            _scanner.Buffer.ReadU16BE();
            _scanner.Buffer.ReadU16BE();

            if (_scanner.Buffer.Remaining < 2)
            {
                return false;
            }

            var constantPoolCount = _scanner.Buffer.ReadU16BE();

            return constantPoolCount > 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     JVM ClassFile 扫描头部信息。
/// </summary>
public sealed class JvmScanHeader
{
    /// <summary>
    ///     次版本号。
    /// </summary>
    public ushort MinorVersion { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public ushort MajorVersion { get; init; }

    /// <summary>
    ///     常量池条目数。
    /// </summary>
    public ushort ConstantPoolCount { get; set; }

    /// <summary>
    ///     类访问标志。
    /// </summary>
    public ushort AccessFlags { get; set; }

    /// <summary>
    ///     本类常量池索引。
    /// </summary>
    public ushort ThisClassIndex { get; set; }

    /// <summary>
    ///     父类常量池索引。
    /// </summary>
    public ushort SuperClassIndex { get; set; }

    /// <summary>
    ///     Java 版本友好名称。
    /// </summary>
    public string JavaVersion => MajorVersion switch
    {
        45 => "Java 1.1",
        46 => "Java 1.2",
        47 => "Java 1.3",
        48 => "Java 1.4",
        49 => "Java 5",
        50 => "Java 6",
        51 => "Java 7",
        52 => "Java 8",
        53 => "Java 9",
        54 => "Java 10",
        55 => "Java 11",
        56 => "Java 12",
        57 => "Java 13",
        58 => "Java 14",
        59 => "Java 15",
        60 => "Java 16",
        61 => "Java 17",
        62 => "Java 18",
        63 => "Java 19",
        64 => "Java 20",
        65 => "Java 21",
        66 => "Java 22",
        67 => "Java 23",
        68 => "Java 24",
        _ => MajorVersion >= 45 ? $"Java {MajorVersion - 44}" : $"未知 ({MajorVersion})"
    };

    /// <summary>
    ///     类是否为接口。
    /// </summary>
    public bool IsInterface => (AccessFlags & 0x0200) != 0;

    /// <summary>
    ///     类是否为抽象类。
    /// </summary>
    public bool IsAbstract => (AccessFlags & 0x0400) != 0;

    /// <summary>
    ///     类是否为注解类型。
    /// </summary>
    public bool IsAnnotation => (AccessFlags & 0x2000) != 0;

    /// <summary>
    ///     类是否为枚举。
    /// </summary>
    public bool IsEnum => (AccessFlags & 0x4000) != 0;

    /// <summary>
    ///     类是否为模块信息。
    /// </summary>
    public bool IsModule => (AccessFlags & 0x8000) != 0;

    /// <summary>
    ///     类类型描述。
    /// </summary>
    public string ClassKind
    {
        get
        {
            if (IsAnnotation)
            {
                return "@interface";
            }

            if (IsInterface)
            {
                return "interface";
            }

            if (IsEnum)
            {
                return "enum";
            }

            if (IsModule)
            {
                return "module-info";
            }

            return "class";
        }
    }

    /// <summary>
    ///     是否为抽象类型（含接口）。
    /// </summary>
    public bool IsAbstractType => IsAbstract || IsInterface;
}

/// <summary>
///     JVM ClassFile 统计信息。
/// </summary>
public sealed class JvmStatistics
{
    /// <summary>
    ///     次版本号。
    /// </summary>
    public ushort MinorVersion { get; set; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public ushort MajorVersion { get; set; }

    /// <summary>
    ///     常量池条目总数。
    /// </summary>
    public ushort ConstantPoolCount { get; set; }

    /// <summary>
    ///     UTF-8 常量数量。
    /// </summary>
    public int Utf8ConstantCount { get; set; }

    /// <summary>
    ///     整数常量数量。
    /// </summary>
    public int IntegerConstantCount { get; set; }

    /// <summary>
    ///     浮点数常量数量。
    /// </summary>
    public int FloatConstantCount { get; set; }

    /// <summary>
    ///     长整数常量数量。
    /// </summary>
    public int LongConstantCount { get; set; }

    /// <summary>
    ///     双精度浮点数常量数量。
    /// </summary>
    public int DoubleConstantCount { get; set; }

    /// <summary>
    ///     类引用常量数量。
    /// </summary>
    public int ClassConstantCount { get; set; }

    /// <summary>
    ///     字符串常量数量。
    /// </summary>
    public int StringConstantCount { get; set; }

    /// <summary>
    ///     字段引用常量数量。
    /// </summary>
    public int FieldrefConstantCount { get; set; }

    /// <summary>
    ///     方法引用常量数量。
    /// </summary>
    public int MethodrefConstantCount { get; set; }

    /// <summary>
    ///     接口方法引用常量数量。
    /// </summary>
    public int InterfaceMethodrefConstantCount { get; set; }

    /// <summary>
    ///     名称和类型常量数量。
    /// </summary>
    public int NameAndTypeConstantCount { get; set; }

    /// <summary>
    ///     方法句柄常量数量。
    /// </summary>
    public int MethodHandleConstantCount { get; set; }

    /// <summary>
    ///     方法类型常量数量。
    /// </summary>
    public int MethodTypeConstantCount { get; set; }

    /// <summary>
    ///     动态调用常量数量。
    /// </summary>
    public int InvokeDynamicConstantCount { get; set; }

    /// <summary>
    ///     类访问标志。
    /// </summary>
    public ushort AccessFlags { get; set; }

    /// <summary>
    ///     接口数量。
    /// </summary>
    public ushort InterfaceCount { get; set; }

    /// <summary>
    ///     字段数量。
    /// </summary>
    public ushort FieldCount { get; set; }

    /// <summary>
    ///     方法数量。
    /// </summary>
    public ushort MethodCount { get; set; }

    /// <summary>
    ///     类级属性数量。
    /// </summary>
    public ushort ClassAttributeCount { get; set; }

    /// <summary>
    ///     方法属性总字节数（含 Code 属性等）。
    /// </summary>
    public uint TotalMethodAttributeBytes { get; set; }

    /// <summary>
    ///     Java 版本友好名称。
    /// </summary>
    public string JavaVersion => MajorVersion switch
    {
        45 => "Java 1.1",
        46 => "Java 1.2",
        47 => "Java 1.3",
        48 => "Java 1.4",
        49 => "Java 5",
        50 => "Java 6",
        51 => "Java 7",
        52 => "Java 8",
        53 => "Java 9",
        54 => "Java 10",
        55 => "Java 11",
        56 => "Java 12",
        57 => "Java 13",
        58 => "Java 14",
        59 => "Java 15",
        60 => "Java 16",
        61 => "Java 17",
        62 => "Java 18",
        63 => "Java 19",
        64 => "Java 20",
        65 => "Java 21",
        66 => "Java 22",
        67 => "Java 23",
        68 => "Java 24",
        _ => MajorVersion >= 45 ? $"Java {MajorVersion - 44}" : $"未知 ({MajorVersion})"
    };
}
