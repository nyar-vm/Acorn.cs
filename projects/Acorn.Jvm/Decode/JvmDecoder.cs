using Acorn.Jvm.Data;
using System;
using System.IO;
using System.Text;

namespace Acorn.Jvm.Decode;

/// <summary>
///     JVM ClassFile 解码器
/// </summary>
public sealed class JvmDecoder
{
    /// <summary>
    ///     从字节数组解码 ClassFile
    /// </summary>
    public JvmClassFileData Decode(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // 读取魔数
        var magic = reader.ReadUInt32();
        if (magic != 0xCAFEBABE)
        {
            throw new DecodeException("Invalid ClassFile magic");
        }

        // 读取版本号
        var minorVersion = reader.ReadUInt16();
        var majorVersion = reader.ReadUInt16();

        // 读取常量池
        var constantPoolCount = reader.ReadUInt16();
        var constantPool = new List<JvmConstant>();
        for (int i = 1; i < constantPoolCount; i++)
        {
            var constant = DecodeConstant(reader);
            constantPool.Add(constant);
            // 长整数和双精度浮点数占用两个常量池项
            if (constant.Kind == JvmConstantKind.Long || constant.Kind == JvmConstantKind.Double)
            {
                i++;
            }
        }

        // 读取访问标志
        var accessFlags = reader.ReadUInt16();

        // 读取类索引
        var thisClass = reader.ReadUInt16();
        var superClass = reader.ReadUInt16();

        // 读取接口
        var interfacesCount = reader.ReadUInt16();
        var interfaces = new List<ushort>();
        for (int i = 0; i < interfacesCount; i++)
        {
            interfaces.Add(reader.ReadUInt16());
        }

        // 读取字段
        var fieldsCount = reader.ReadUInt16();
        var fields = new List<JvmFieldInfo>();
        for (int i = 0; i < fieldsCount; i++)
        {
            fields.Add(DecodeFieldInfo(reader));
        }

        // 读取方法
        var methodsCount = reader.ReadUInt16();
        var methods = new List<JvmMethodInfo>();
        for (int i = 0; i < methodsCount; i++)
        {
            methods.Add(DecodeMethodInfo(reader));
        }

        // 读取属性
        var attributesCount = reader.ReadUInt16();
        var attributes = new List<JvmAttributeInfo>();
        for (int i = 0; i < attributesCount; i++)
        {
            var attr = DecodeAttributeInfo(reader);
            if (attr is not null) attributes.Add(attr);
        }

        return new JvmClassFileData
        {
            Magic = magic,
            MinorVersion = minorVersion,
            MajorVersion = majorVersion,
            ConstantPool = constantPool,
            AccessFlags = accessFlags,
            ThisClass = thisClass,
            SuperClass = superClass,
            Interfaces = interfaces,
            Fields = fields,
            Methods = methods,
            Attributes = attributes
        };
    }

    /// <summary>
    ///     解码常量池项
    /// </summary>
    private JvmConstant DecodeConstant(BinaryReader reader)
    {
        var tag = reader.ReadByte();
        return tag switch
        {
            (byte)JvmConstantKind.Utf8 => new JvmConstantUtf8
            {
                Value = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()))
            },
            (byte)JvmConstantKind.Integer => new JvmConstantInteger
            {
                Value = reader.ReadInt32()
            },
            (byte)JvmConstantKind.Float => new JvmConstantFloat
            {
                Value = reader.ReadSingle()
            },
            (byte)JvmConstantKind.Long => new JvmConstantLong
            {
                Value = reader.ReadInt64()
            },
            (byte)JvmConstantKind.Double => new JvmConstantDouble
            {
                Value = reader.ReadDouble()
            },
            (byte)JvmConstantKind.Class => new JvmConstantClass
            {
                NameIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.String => new JvmConstantString
            {
                StringIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.Fieldref => new JvmConstantFieldref
            {
                ClassIndex = reader.ReadUInt16(),
                NameAndTypeIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.Methodref => new JvmConstantMethodref
            {
                ClassIndex = reader.ReadUInt16(),
                NameAndTypeIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.InterfaceMethodref => new JvmConstantInterfaceMethodref
            {
                ClassIndex = reader.ReadUInt16(),
                NameAndTypeIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.NameAndType => new JvmConstantNameAndType
            {
                NameIndex = reader.ReadUInt16(),
                DescriptorIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.MethodHandle => new JvmConstantMethodHandle
            {
                ReferenceKind = reader.ReadByte(),
                ReferenceIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.MethodType => new JvmConstantMethodType
            {
                DescriptorIndex = reader.ReadUInt16()
            },
            (byte)JvmConstantKind.InvokeDynamic => new JvmConstantInvokeDynamic
            {
                BootstrapMethodAttrIndex = reader.ReadUInt16(),
                NameAndTypeIndex = reader.ReadUInt16()
            },
            _ => throw new DecodeException($"Unknown constant tag: {tag}")
        };
    }

    /// <summary>
    ///     解码字段信息
    /// </summary>
    private JvmFieldInfo DecodeFieldInfo(BinaryReader reader)
    {
        var accessFlags = reader.ReadUInt16();
        var nameIndex = reader.ReadUInt16();
        var descriptorIndex = reader.ReadUInt16();
        var attributesCount = reader.ReadUInt16();
        var attributes = new List<JvmAttributeInfo>();
        for (int i = 0; i < attributesCount; i++)
        {
            var attr = DecodeAttributeInfo(reader);
            if (attr is not null) attributes.Add(attr);
        }

        return new JvmFieldInfo
        {
            AccessFlags = accessFlags,
            NameIndex = nameIndex,
            DescriptorIndex = descriptorIndex,
            Attributes = attributes
        };
    }

    /// <summary>
    ///     解码方法信息
    /// </summary>
    private JvmMethodInfo DecodeMethodInfo(BinaryReader reader)
    {
        var accessFlags = reader.ReadUInt16();
        var nameIndex = reader.ReadUInt16();
        var descriptorIndex = reader.ReadUInt16();
        var attributesCount = reader.ReadUInt16();
        var attributes = new List<JvmAttributeInfo>();
        for (int i = 0; i < attributesCount; i++)
        {
            var attr = DecodeAttributeInfo(reader);
            if (attr is not null) attributes.Add(attr);
        }

        return new JvmMethodInfo
        {
            AccessFlags = accessFlags,
            NameIndex = nameIndex,
            DescriptorIndex = descriptorIndex,
            Attributes = attributes
        };
    }

    /// <summary>
    ///     解码属性信息
    /// </summary>
    private JvmAttributeInfo? DecodeAttributeInfo(BinaryReader reader)
    {
        var attributeNameIndex = reader.ReadUInt16();
        var attributeLength = reader.ReadUInt32();

        // 简化实现，返回 null
        return null;
    }
}
