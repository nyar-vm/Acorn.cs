using Acorn.Jvm.Data;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Acorn.Jvm.Decode;

/// <summary>
///     JVM ClassFile 解码器，将 .class 二进制解码为 <see cref="JvmClassFileData" />
/// </summary>
public sealed class JvmDecoder
{
    /// <summary>
    ///     从字节数组解码 ClassFile
    /// </summary>
    public JvmClassFileData Decode(byte[] data)
    {
        var span = new ReadOnlySpan<byte>(data);
        var offset = 0;

        var magic = BinaryPrimitives.ReadUInt32BigEndian(span);
        offset += 4;
        if (magic != JvmConstants.Magic)
        {
            throw new InvalidDataException("非法的 ClassFile 魔数");
        }

        var minorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var majorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;

        var constantPoolCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var constantPool = new List<JvmConstant>();
        var utf8Lookup = new Dictionary<ushort, string>();
        for (var i = 1; i < constantPoolCount; i++)
        {
            var (constant, consumed) = DecodeConstant(span[offset..]);
            constantPool.Add(constant);
            offset += consumed;
            if (constant is JvmConstantUtf8 utf8)
            {
                utf8Lookup[(ushort)i] = utf8.Value;
            }

            if (constant.Kind is JvmConstantKind.Long or JvmConstantKind.Double)
            {
                i++;
            }
        }

        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var thisClass = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var superClass = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;

        var interfacesCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var interfaces = new List<ushort>();
        for (var i = 0; i < interfacesCount; i++)
        {
            interfaces.Add(BinaryPrimitives.ReadUInt16BigEndian(span[offset..]));
            offset += 2;
        }

        var fieldsCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var fields = new List<JvmFieldInfo>();
        for (var i = 0; i < fieldsCount; i++)
        {
            var (field, consumed) = DecodeFieldInfo(span[offset..], utf8Lookup);
            fields.Add(field);
            offset += consumed;
        }

        var methodsCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var methods = new List<JvmMethodInfo>();
        for (var i = 0; i < methodsCount; i++)
        {
            var (method, consumed) = DecodeMethodInfo(span[offset..], utf8Lookup);
            methods.Add(method);
            offset += consumed;
        }

        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = DecodeAttributeInfo(span[offset..], utf8Lookup);
            if (attr is not null)
            {
                attributes.Add(attr);
            }

            offset += consumed;
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

    private static (JvmConstant Constant, int Consumed) DecodeConstant(ReadOnlySpan<byte> data)
    {
        var tag = data[0];
        var offset = 1;
        return tag switch
        {
            (byte)JvmConstantKind.Utf8 => (new JvmConstantUtf8
            {
                Value = System.Text.Encoding.UTF8.GetString(
                    data.Slice(offset + 2, BinaryPrimitives.ReadUInt16BigEndian(data[offset..])))
            }, offset + 2 + BinaryPrimitives.ReadUInt16BigEndian(data[offset..])),
            (byte)JvmConstantKind.Integer => (new JvmConstantInteger
            {
                Value = BinaryPrimitives.ReadInt32BigEndian(data[offset..])
            }, offset + 4),
            (byte)JvmConstantKind.Float => (new JvmConstantFloat
            {
                Value = BinaryPrimitives.ReadSingleBigEndian(data[offset..])
            }, offset + 4),
            (byte)JvmConstantKind.Long => (new JvmConstantLong
            {
                Value = BinaryPrimitives.ReadInt64BigEndian(data[offset..])
            }, offset + 8),
            (byte)JvmConstantKind.Double => (new JvmConstantDouble
            {
                Value = BinaryPrimitives.ReadDoubleBigEndian(data[offset..])
            }, offset + 8),
            (byte)JvmConstantKind.Class => (new JvmConstantClass
            {
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.String => (new JvmConstantString
            {
                StringIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.Fieldref => (new JvmConstantFieldref
            {
                ClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.Methodref => (new JvmConstantMethodref
            {
                ClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.InterfaceMethodref => (new JvmConstantInterfaceMethodref
            {
                ClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.NameAndType => (new JvmConstantNameAndType
            {
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                DescriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.MethodHandle => (new JvmConstantMethodHandle
            {
                ReferenceKind = data[offset],
                ReferenceIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 1)..])
            }, offset + 3),
            (byte)JvmConstantKind.MethodType => (new JvmConstantMethodType
            {
                DescriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.InvokeDynamic => (new JvmConstantInvokeDynamic
            {
                BootstrapMethodAttrIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            _ => throw new InvalidDataException($"未知的常量池标记: 0x{tag:X2}")
        };
    }

    private static (JvmFieldInfo Field, int Consumed) DecodeFieldInfo(ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = DecodeAttributeInfo(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null)
            {
                attributes.Add(attr);
            }
        }

        return (new JvmFieldInfo
        {
            AccessFlags = accessFlags,
            NameIndex = nameIndex,
            DescriptorIndex = descriptorIndex,
            Attributes = attributes
        }, offset);
    }

    private static (JvmMethodInfo Method, int Consumed) DecodeMethodInfo(ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = DecodeAttributeInfo(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null)
            {
                attributes.Add(attr);
            }
        }

        return (new JvmMethodInfo
        {
            AccessFlags = accessFlags,
            NameIndex = nameIndex,
            DescriptorIndex = descriptorIndex,
            Attributes = attributes
        }, offset);
    }

    #region 属性解码

    /// <summary>
    ///     根据常量池中解析的属性名称，分发解码已知属性
    /// </summary>
    private static (JvmAttributeInfo? Attribute, int Consumed) DecodeAttributeInfo(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var attributeNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var attributeLength = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;
        var attributeDataStart = offset;

        var attributeName = utf8Lookup.GetValueOrDefault(attributeNameIndex);

        JvmAttributeInfo? attribute;
        switch (attributeName)
        {
            case "Code":
                attribute = DecodeCodeAttribute(data[offset..], utf8Lookup, attributeNameIndex, attributeLength);
                break;
            case "LineNumberTable":
                attribute = DecodeLineNumberTable(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "LocalVariableTable":
                attribute = DecodeLocalVariableTable(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "ConstantValue":
                attribute = DecodeConstantValue(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "SourceFile":
                attribute = DecodeSourceFile(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Exceptions":
                attribute = DecodeExceptions(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Signature":
                attribute = ReadRawBytes(data[offset..], (int)attributeLength, "Signature", attributeNameIndex, attributeLength);
                break;
            default:
                attribute = null;
                break;
        }

        offset = attributeDataStart + (int)attributeLength;
        return (attribute, offset);
    }

    private static JvmCodeAttribute DecodeCodeAttribute(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var maxStack = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var maxLocals = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var codeLength = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;
        var code = data.Slice(offset, (int)codeLength).ToArray();
        offset += (int)codeLength;
        var exceptionTableLength = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var exceptionTable = new List<JvmExceptionTableEntry>();
        for (var i = 0; i < exceptionTableLength; i++)
        {
            exceptionTable.Add(new JvmExceptionTableEntry
            {
                StartPc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                EndPc = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                HandlerPc = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                CatchType = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..])
            });
            offset += 8;
        }

        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = DecodeAttributeInfo(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null)
            {
                attributes.Add(attr);
            }
        }

        return new JvmCodeAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            MaxStack = maxStack,
            MaxLocals = maxLocals,
            CodeLength = codeLength,
            Code = code,
            ExceptionTableLength = exceptionTableLength,
            ExceptionTable = exceptionTable,
            AttributesCount = attributesCount,
            Attributes = attributes
        };
    }

    private static JvmLineNumberTableAttribute DecodeLineNumberTable(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLineNumberEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLineNumberEntry
            {
                StartPc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                LineNumber = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            });
            offset += 4;
        }

        return new JvmLineNumberTableAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            LineNumberTableLength = tableLength,
            LineNumberTable = table
        };
    }

    private static JvmLocalVariableTableAttribute DecodeLocalVariableTable(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLocalVariableEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLocalVariableEntry
            {
                StartPc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                Length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                DescriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..]),
                Index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 8)..])
            });
            offset += 10;
        }

        return new JvmLocalVariableTableAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            LocalVariableTableLength = tableLength,
            LocalVariableTable = table
        };
    }

    private static JvmConstantValueAttribute DecodeConstantValue(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmConstantValueAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            ConstantValueIndex = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmSourceFileAttribute DecodeSourceFile(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSourceFileAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            SourceFileIndex = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmExceptionsAttribute DecodeExceptions(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var count = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var exceptions = new List<ushort>();
        for (var i = 0; i < count; i++)
        {
            exceptions.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmExceptionsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumberOfExceptions = count,
            ExceptionIndexTable = exceptions
        };
    }

    /// <summary>
    ///     解码为原始字节属性（未识别的格式保留原始数据）
    /// </summary>
    private static JvmRawAttribute ReadRawBytes(
        ReadOnlySpan<byte> data, int length, string name, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmRawAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            Name = name,
            RawData = data[..length].ToArray()
        };
    }

    #endregion
}
