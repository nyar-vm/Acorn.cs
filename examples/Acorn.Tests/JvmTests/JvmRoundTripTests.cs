using Acorn.Jvm.Data;
using Acorn.Jvm.Decode;
using Acorn.Jvm.Encode;

namespace Acorn.Tests.JvmTests;

public class JvmRoundTripTests
{
    /// <summary>
    ///     最小 ClassFile 往返测试：使用仅含基本字段的最小 JvmClassFileData 编码后解码，验证所有字段一致
    /// </summary>
    [Fact]
    public void Encode_Decode_MinimalClassFile()
    {
        var original = new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 61,
            ConstantPool = [],
            AccessFlags = 0,
            ThisClass = 0,
            SuperClass = 0,
            Interfaces = [],
            Fields = [],
            Methods = [],
            Attributes = []
        };

        var encoder = new JvmEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new JvmDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Magic, decoded.Magic);
        Assert.Equal(original.MinorVersion, decoded.MinorVersion);
        Assert.Equal(original.MajorVersion, decoded.MajorVersion);
        Assert.Empty(decoded.ConstantPool);
        Assert.Equal(original.AccessFlags, decoded.AccessFlags);
        Assert.Equal(original.ThisClass, decoded.ThisClass);
        Assert.Equal(original.SuperClass, decoded.SuperClass);
        Assert.Empty(decoded.Interfaces);
        Assert.Empty(decoded.Fields);
        Assert.Empty(decoded.Methods);
        Assert.Empty(decoded.Attributes);
    }

    /// <summary>
    ///     完整 ClassFile 往返测试：包含多种常量池条目（Utf8、Class、MethodRef、FieldRef、NameAndType、
    ///     String、Integer、Float、MethodType、MethodHandle、InterfaceMethodref、InvokeDynamic）、
    ///     字段、方法，编码后解码，验证所有内容完全还原
    /// </summary>
    [Fact]
    public void Encode_Decode_FullClassFile()
    {
        var constantPool = new JvmConstant[]
        {
            new JvmConstantUtf8 { Value = "java/lang/Object" },
            new JvmConstantUtf8 { Value = "<init>" },
            new JvmConstantUtf8 { Value = "()V" },
            new JvmConstantUtf8 { Value = "Code" },
            new JvmConstantUtf8 { Value = "hello world" },
            new JvmConstantUtf8 { Value = "I" },
            new JvmConstantUtf8 { Value = "testField" },
            new JvmConstantUtf8 { Value = "testMethod" },
            new JvmConstantUtf8 { Value = "(I)I" },
            new JvmConstantClass { NameIndex = 1 },
            new JvmConstantNameAndType { NameIndex = 2, DescriptorIndex = 3 },
            new JvmConstantMethodref { ClassIndex = 10, NameAndTypeIndex = 11 },
            new JvmConstantNameAndType { NameIndex = 7, DescriptorIndex = 6 },
            new JvmConstantFieldref { ClassIndex = 10, NameAndTypeIndex = 13 },
            new JvmConstantString { StringIndex = 5 },
            new JvmConstantInteger { Value = 42 },
            new JvmConstantFloat { Value = 3.14f },
            new JvmConstantMethodType { DescriptorIndex = 3 },
            new JvmConstantMethodHandle { ReferenceKind = 6, ReferenceIndex = 12 },
            new JvmConstantInterfaceMethodref { ClassIndex = 10, NameAndTypeIndex = 11 },
            new JvmConstantInvokeDynamic { BootstrapMethodAttrIndex = 0, NameAndTypeIndex = 13 }
        };

        var fields = new JvmFieldInfo[]
        {
            new()
            {
                AccessFlags = 0,
                NameIndex = 7,
                DescriptorIndex = 6,
                Attributes = []
            }
        };

        var methods = new JvmMethodInfo[]
        {
            new()
            {
                AccessFlags = 0,
                NameIndex = 2,
                DescriptorIndex = 3,
                Attributes = []
            },
            new()
            {
                AccessFlags = 0,
                NameIndex = 8,
                DescriptorIndex = 9,
                Attributes = []
            }
        };

        var original = new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 61,
            ConstantPool = constantPool,
            AccessFlags = 0x0021,
            ThisClass = 10,
            SuperClass = 10,
            Interfaces = [],
            Fields = fields,
            Methods = methods,
            Attributes = []
        };

        var encoder = new JvmEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new JvmDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Magic, decoded.Magic);
        Assert.Equal(original.MinorVersion, decoded.MinorVersion);
        Assert.Equal(original.MajorVersion, decoded.MajorVersion);

        Assert.Equal(constantPool.Length, decoded.ConstantPool.Count);

        var decodedUtf8_1 = Assert.IsType<JvmConstantUtf8>(decoded.ConstantPool[0]);
        Assert.Equal("java/lang/Object", decodedUtf8_1.Value);

        var decodedUtf8_5 = Assert.IsType<JvmConstantUtf8>(decoded.ConstantPool[4]);
        Assert.Equal("hello world", decodedUtf8_5.Value);

        var decodedClass = Assert.IsType<JvmConstantClass>(decoded.ConstantPool[9]);
        Assert.Equal((ushort)1, decodedClass.NameIndex);

        var decodedNat = Assert.IsType<JvmConstantNameAndType>(decoded.ConstantPool[10]);
        Assert.Equal((ushort)2, decodedNat.NameIndex);
        Assert.Equal((ushort)3, decodedNat.DescriptorIndex);

        var decodedMethodref = Assert.IsType<JvmConstantMethodref>(decoded.ConstantPool[11]);
        Assert.Equal((ushort)10, decodedMethodref.ClassIndex);
        Assert.Equal((ushort)11, decodedMethodref.NameAndTypeIndex);

        var decodedFieldref = Assert.IsType<JvmConstantFieldref>(decoded.ConstantPool[13]);
        Assert.Equal((ushort)10, decodedFieldref.ClassIndex);
        Assert.Equal((ushort)13, decodedFieldref.NameAndTypeIndex);

        var decodedString = Assert.IsType<JvmConstantString>(decoded.ConstantPool[14]);
        Assert.Equal((ushort)5, decodedString.StringIndex);

        var decodedInteger = Assert.IsType<JvmConstantInteger>(decoded.ConstantPool[15]);
        Assert.Equal(42, decodedInteger.Value);

        var decodedFloat = Assert.IsType<JvmConstantFloat>(decoded.ConstantPool[16]);
        Assert.Equal(3.14f, decodedFloat.Value, 2);

        var decodedMethodType = Assert.IsType<JvmConstantMethodType>(decoded.ConstantPool[17]);
        Assert.Equal((ushort)3, decodedMethodType.DescriptorIndex);

        var decodedMethodHandle = Assert.IsType<JvmConstantMethodHandle>(decoded.ConstantPool[18]);
        Assert.Equal((byte)6, decodedMethodHandle.ReferenceKind);
        Assert.Equal((ushort)12, decodedMethodHandle.ReferenceIndex);

        var decodedImethodref = Assert.IsType<JvmConstantInterfaceMethodref>(decoded.ConstantPool[19]);
        Assert.Equal((ushort)10, decodedImethodref.ClassIndex);
        Assert.Equal((ushort)11, decodedImethodref.NameAndTypeIndex);

        var decodedInvokeDynamic = Assert.IsType<JvmConstantInvokeDynamic>(decoded.ConstantPool[20]);
        Assert.Equal((ushort)0, decodedInvokeDynamic.BootstrapMethodAttrIndex);
        Assert.Equal((ushort)13, decodedInvokeDynamic.NameAndTypeIndex);

        Assert.Equal(original.AccessFlags, decoded.AccessFlags);
        Assert.Equal(original.ThisClass, decoded.ThisClass);
        Assert.Equal(original.SuperClass, decoded.SuperClass);

        Assert.Empty(decoded.Interfaces);

        Assert.Equal(fields.Length, decoded.Fields.Count);
        Assert.Equal(fields[0].AccessFlags, decoded.Fields[0].AccessFlags);
        Assert.Equal(fields[0].NameIndex, decoded.Fields[0].NameIndex);
        Assert.Equal(fields[0].DescriptorIndex, decoded.Fields[0].DescriptorIndex);

        Assert.Equal(methods.Length, decoded.Methods.Count);
        Assert.Equal(methods[0].AccessFlags, decoded.Methods[0].AccessFlags);
        Assert.Equal(methods[0].NameIndex, decoded.Methods[0].NameIndex);
        Assert.Equal(methods[0].DescriptorIndex, decoded.Methods[0].DescriptorIndex);
        Assert.Equal(methods[1].AccessFlags, decoded.Methods[1].AccessFlags);
        Assert.Equal(methods[1].NameIndex, decoded.Methods[1].NameIndex);
        Assert.Equal(methods[1].DescriptorIndex, decoded.Methods[1].DescriptorIndex);
    }

    /// <summary>
    ///     无效魔数解码失败：使用不包含 CAFEBABE 魔数的字节数组解码，验证抛出异常
    /// </summary>
    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[32];
        var decoder = new JvmDecoder();

        var ex = Assert.ThrowsAny<Exception>(() => decoder.Decode(data));
        Assert.Contains("非法的 ClassFile 魔数", ex.Message);
    }

    /// <summary>
    ///     截断数据解码失败：使用过短（不足 4 字节）的字节数组解码，验证抛出异常
    /// </summary>
    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[4];
        var decoder = new JvmDecoder();

        Assert.ThrowsAny<Exception>(() => decoder.Decode(data));
    }
}
