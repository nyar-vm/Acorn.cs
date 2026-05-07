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

    #region 常量池解码

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
            (byte)JvmConstantKind.Dynamic => (new JvmConstantDynamic
            {
                BootstrapMethodAttrIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.InvokeDynamic => (new JvmConstantInvokeDynamic
            {
                BootstrapMethodAttrIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.Module => (new JvmConstantModule
            {
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.Package => (new JvmConstantPackage
            {
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            _ => throw new InvalidDataException($"未知的常量池标记: 0x{tag:X2}")
        };
    }

    #endregion

    #region 字段/方法解码

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

    #endregion

    #region 属性解码

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
            case "LocalVariableTypeTable":
                attribute = DecodeLocalVariableTypeTable(data[offset..], attributeNameIndex, attributeLength);
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
            case "InnerClasses":
                attribute = DecodeInnerClasses(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "BootstrapMethods":
                attribute = DecodeBootstrapMethods(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Signature":
                attribute = DecodeSignature(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Synthetic":
                attribute = new JvmSyntheticAttribute
                {
                    AttributeNameIndex = attributeNameIndex,
                    AttributeLength = attributeLength
                };
                break;
            case "Deprecated":
                attribute = new JvmDeprecatedAttribute
                {
                    AttributeNameIndex = attributeNameIndex,
                    AttributeLength = attributeLength
                };
                break;
            case "EnclosingMethod":
                attribute = DecodeEnclosingMethod(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "SourceDebugExtension":
                attribute = DecodeSourceDebugExtension(data[offset..], (int)attributeLength, attributeNameIndex, attributeLength);
                break;
            case "MethodParameters":
                attribute = DecodeMethodParameters(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "StackMapTable":
                attribute = DecodeStackMapTable(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "NestHost":
                attribute = DecodeNestHost(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "NestMembers":
                attribute = DecodeNestMembers(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Record":
                attribute = DecodeRecord(data[offset..], utf8Lookup, attributeNameIndex, attributeLength);
                break;
            case "PermittedSubclasses":
                attribute = DecodePermittedSubclasses(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeVisibleAnnotations":
                attribute = DecodeRuntimeVisibleAnnotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeInvisibleAnnotations":
                attribute = DecodeRuntimeInvisibleAnnotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeVisibleParameterAnnotations":
                attribute = DecodeRuntimeVisibleParameterAnnotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeInvisibleParameterAnnotations":
                attribute = DecodeRuntimeInvisibleParameterAnnotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "AnnotationDefault":
                attribute = DecodeAnnotationDefault(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Module":
                attribute = DecodeModule(data[offset..], attributeNameIndex, attributeLength);
                break;
            default:
                attribute = ReadRawBytes(data[offset..], (int)attributeLength, attributeName ?? "Unknown", attributeNameIndex, attributeLength);
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

    private static JvmLocalVariableTypeTableAttribute DecodeLocalVariableTypeTable(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLocalVariableTypeEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLocalVariableTypeEntry
            {
                StartPc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                Length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                SignatureIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..]),
                Index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 8)..])
            });
            offset += 10;
        }

        return new JvmLocalVariableTypeTableAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            LocalVariableTypeTableLength = tableLength,
            LocalVariableTypeTable = table
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

    private static JvmInnerClassesAttribute DecodeInnerClasses(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<JvmInnerClassInfo>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(new JvmInnerClassInfo
            {
                InnerClassInfoIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                OuterClassInfoIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                InnerNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                InnerClassAccessFlags = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..])
            });
            offset += 8;
        }

        return new JvmInnerClassesAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumberOfClasses = numberOfClasses,
            Classes = classes
        };
    }

    private static JvmBootstrapMethodsAttribute DecodeBootstrapMethods(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numBootstrapMethods = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var methods = new List<JvmBootstrapMethod>();
        for (var i = 0; i < numBootstrapMethods; i++)
        {
            var methodRef = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var numArgs = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var args = new List<ushort>();
            for (var j = 0; j < numArgs; j++)
            {
                args.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            methods.Add(new JvmBootstrapMethod
            {
                BootstrapMethodRef = methodRef,
                NumBootstrapArguments = numArgs,
                BootstrapArguments = args
            });
        }

        return new JvmBootstrapMethodsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumBootstrapMethods = numBootstrapMethods,
            BootstrapMethods = methods
        };
    }

    private static JvmSignatureAttribute DecodeSignature(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSignatureAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            SignatureIndex = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmEnclosingMethodAttribute DecodeEnclosingMethod(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmEnclosingMethodAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            ClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data),
            MethodIndex = BinaryPrimitives.ReadUInt16BigEndian(data[2..])
        };
    }

    private static JvmSourceDebugExtensionAttribute DecodeSourceDebugExtension(
        ReadOnlySpan<byte> data, int length, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSourceDebugExtensionAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            DebugExtension = data[..length].ToArray()
        };
    }

    private static JvmMethodParametersAttribute DecodeMethodParameters(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var parametersCount = data[offset];
        offset += 1;
        var parameters = new List<JvmMethodParameterInfo>();
        for (var i = 0; i < parametersCount; i++)
        {
            parameters.Add(new JvmMethodParameterInfo
            {
                NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                AccessFlags = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            });
            offset += 4;
        }

        return new JvmMethodParametersAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            ParameterCount = parametersCount,
            Parameters = parameters
        };
    }

    private static JvmNestHostAttribute DecodeNestHost(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmNestHostAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            HostClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmNestMembersAttribute DecodeNestMembers(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<ushort>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmNestMembersAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumberOfClasses = numberOfClasses,
            ClassIndexes = classes
        };
    }

    private static JvmPermittedSubclassesAttribute DecodePermittedSubclasses(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<ushort>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmPermittedSubclassesAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumberOfClasses = numberOfClasses,
            ClassIndexes = classes
        };
    }

    private static JvmRecordAttribute DecodeRecord(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var componentsCount = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var components = new List<JvmRecordComponentInfo>();
        for (var i = 0; i < componentsCount; i++)
        {
            var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var attrCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var attrs = new List<JvmAttributeInfo>();
            for (var j = 0; j < attrCount; j++)
            {
                var (attr, consumed) = DecodeAttributeInfo(data[offset..], utf8Lookup);
                offset += consumed;
                if (attr is not null)
                {
                    attrs.Add(attr);
                }
            }

            components.Add(new JvmRecordComponentInfo
            {
                NameIndex = nameIndex,
                DescriptorIndex = descriptorIndex,
                Attributes = attrs
            });
        }

        return new JvmRecordAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            ComponentsCount = componentsCount,
            Components = components
        };
    }

    private static JvmStackMapTableAttribute DecodeStackMapTable(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfEntries = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var entries = new List<JvmStackMapFrame>();
        for (var i = 0; i < numberOfEntries; i++)
        {
            var frame = DecodeStackMapFrame(data, ref offset);
            entries.Add(frame);
        }

        return new JvmStackMapTableAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumberOfEntries = numberOfEntries,
            Entries = entries
        };
    }

    private static JvmStackMapFrame DecodeStackMapFrame(ReadOnlySpan<byte> data, ref int offset)
    {
        var frameType = data[offset];
        offset += 1;

        if (frameType <= 63)
        {
            return new JvmSameFrame { FrameType = frameType };
        }

        if (frameType <= 127)
        {
            var stack = DecodeVerificationTypeInfoList(data, ref offset, 1);
            return new JvmSameLocals1StackItemFrame
            {
                FrameType = frameType,
                Stack = stack
            };
        }

        if (frameType <= 246)
        {
            offset += 2;
            return new JvmSameFrame { FrameType = frameType };
        }

        if (frameType is 247)
        {
            offset += 2;
            return new JvmSameFrame { FrameType = frameType };
        }

        if (frameType is >= 248 and <= 250)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            return new JvmChopFrame
            {
                FrameType = frameType,
                OffsetDelta = offsetDelta
            };
        }

        if (frameType is 251)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            return new JvmSameFrameExtended
            {
                FrameType = frameType,
                OffsetDelta = offsetDelta
            };
        }

        if (frameType is >= 252 and <= 254)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var count = frameType - 251;
            var locals = DecodeVerificationTypeInfoList(data, ref offset, count);
            return new JvmAppendFrame
            {
                FrameType = frameType,
                OffsetDelta = offsetDelta,
                Locals = locals
            };
        }

        if (frameType == 255)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var numberOfLocals = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var locals = DecodeVerificationTypeInfoList(data, ref offset, numberOfLocals);
            var numberOfStackItems = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var stack = DecodeVerificationTypeInfoList(data, ref offset, numberOfStackItems);
            return new JvmFullFrame
            {
                FrameType = frameType,
                OffsetDelta = offsetDelta,
                NumberOfLocals = numberOfLocals,
                Locals = locals,
                NumberOfStackItems = numberOfStackItems,
                Stack = stack
            };
        }

        return new JvmSameFrame { FrameType = frameType };
    }

    private static List<JvmVerificationTypeInfo> DecodeVerificationTypeInfoList(
        ReadOnlySpan<byte> data, ref int offset, int count)
    {
        var result = new List<JvmVerificationTypeInfo>();
        for (var i = 0; i < count; i++)
        {
            var tag = data[offset];
            offset += 1;
            ushort? cpoolIndex = null;
            ushort? vtiOffset = null;

            switch (tag)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    cpoolIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                    offset += 2;
                    break;
                case 8:
                    vtiOffset = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                    offset += 2;
                    break;
            }

            result.Add(new JvmVerificationTypeInfo
            {
                Tag = tag,
                CpoolIndex = cpoolIndex,
                Offset = vtiOffset
            });
        }

        return result;
    }

    private static JvmRuntimeVisibleAnnotationsAttribute DecodeRuntimeVisibleAnnotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var annotations = new List<JvmAnnotation>();
        for (var i = 0; i < numAnnotations; i++)
        {
            annotations.Add(DecodeAnnotation(data, ref offset));
        }

        return new JvmRuntimeVisibleAnnotationsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumAnnotations = numAnnotations,
            Annotations = annotations
        };
    }

    private static JvmRuntimeInvisibleAnnotationsAttribute DecodeRuntimeInvisibleAnnotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var annotations = new List<JvmAnnotation>();
        for (var i = 0; i < numAnnotations; i++)
        {
            annotations.Add(DecodeAnnotation(data, ref offset));
        }

        return new JvmRuntimeInvisibleAnnotationsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumAnnotations = numAnnotations,
            Annotations = annotations
        };
    }

    private static JvmRuntimeVisibleParameterAnnotationsAttribute DecodeRuntimeVisibleParameterAnnotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numParameters = data[offset];
        offset += 1;
        var parameterAnnotations = new List<JvmParameterAnnotations>();
        for (var i = 0; i < numParameters; i++)
        {
            var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var annotations = new List<JvmAnnotation>();
            for (var j = 0; j < numAnnotations; j++)
            {
                annotations.Add(DecodeAnnotation(data, ref offset));
            }

            parameterAnnotations.Add(new JvmParameterAnnotations
            {
                NumAnnotations = numAnnotations,
                Annotations = annotations
            });
        }

        return new JvmRuntimeVisibleParameterAnnotationsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumParameters = numParameters,
            ParameterAnnotations = parameterAnnotations
        };
    }

    private static JvmRuntimeInvisibleParameterAnnotationsAttribute DecodeRuntimeInvisibleParameterAnnotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numParameters = data[offset];
        offset += 1;
        var parameterAnnotations = new List<JvmParameterAnnotations>();
        for (var i = 0; i < numParameters; i++)
        {
            var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var annotations = new List<JvmAnnotation>();
            for (var j = 0; j < numAnnotations; j++)
            {
                annotations.Add(DecodeAnnotation(data, ref offset));
            }

            parameterAnnotations.Add(new JvmParameterAnnotations
            {
                NumAnnotations = numAnnotations,
                Annotations = annotations
            });
        }

        return new JvmRuntimeInvisibleParameterAnnotationsAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            NumParameters = numParameters,
            ParameterAnnotations = parameterAnnotations
        };
    }

    private static JvmAnnotationDefaultAttribute DecodeAnnotationDefault(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var defaultValue = DecodeElementValue(data, ref offset);
        return new JvmAnnotationDefaultAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            DefaultValue = defaultValue
        };
    }

    private static JvmModuleAttribute DecodeModule(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var moduleNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var moduleFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var moduleVersionIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        var requiresCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var requires = new List<JvmModuleRequire>();
        for (var i = 0; i < requiresCount; i++)
        {
            var requiresIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var requiresFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var requiresVersionIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            requires.Add(new JvmModuleRequire
            {
                RequiresIndex = requiresIndex,
                RequiresFlags = requiresFlags,
                RequiresVersionIndex = requiresVersionIndex
            });
        }

        var exportsCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var exports = new List<JvmModuleExport>();
        for (var i = 0; i < exportsCount; i++)
        {
            var exportsIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsToCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsTo = new List<ushort>();
            for (var j = 0; j < exportsToCount; j++)
            {
                exportsTo.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            exports.Add(new JvmModuleExport
            {
                ExportsIndex = exportsIndex,
                ExportsFlags = exportsFlags,
                ExportsToIndex = exportsTo
            });
        }

        var opensCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var opens = new List<JvmModuleOpen>();
        for (var i = 0; i < opensCount; i++)
        {
            var opensIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensToCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensTo = new List<ushort>();
            for (var j = 0; j < opensToCount; j++)
            {
                opensTo.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            opens.Add(new JvmModuleOpen
            {
                OpensIndex = opensIndex,
                OpensFlags = opensFlags,
                OpensToIndex = opensTo
            });
        }

        var usesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var usesIndex = new List<ushort>();
        for (var i = 0; i < usesCount; i++)
        {
            usesIndex.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        var providesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var provides = new List<JvmModuleProvide>();
        for (var i = 0; i < providesCount; i++)
        {
            var providesIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var providesWithCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var providesWith = new List<ushort>();
            for (var j = 0; j < providesWithCount; j++)
            {
                providesWith.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            provides.Add(new JvmModuleProvide
            {
                ProvidesIndex = providesIndex,
                ProvidesWithIndex = providesWith
            });
        }

        return new JvmModuleAttribute
        {
            AttributeNameIndex = attributeNameIndex,
            AttributeLength = attributeLength,
            ModuleNameIndex = moduleNameIndex,
            ModuleFlags = moduleFlags,
            ModuleVersionIndex = moduleVersionIndex,
            Requires = requires,
            Exports = exports,
            Opens = opens,
            UsesIndex = usesIndex,
            Provides = provides
        };
    }

    private static JvmAnnotation DecodeAnnotation(ReadOnlySpan<byte> data, ref int offset)
    {
        var typeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var numElementValuePairs = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var pairs = new List<JvmElementValuePair>();
        for (var i = 0; i < numElementValuePairs; i++)
        {
            var elementNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var value = DecodeElementValue(data, ref offset);
            pairs.Add(new JvmElementValuePair
            {
                ElementNameIndex = elementNameIndex,
                Value = value
            });
        }

        return new JvmAnnotation
        {
            TypeIndex = typeIndex,
            NumElementValuePairs = numElementValuePairs,
            ElementValuePairs = pairs
        };
    }

    private static JvmElementValue DecodeElementValue(ReadOnlySpan<byte> data, ref int offset)
    {
        var tag = (char)data[offset];
        offset += 1;

        ushort? constValueIndex = null;
        ushort? typeNameIndex = null;
        ushort? classInfoIndex = null;
        JvmAnnotation? annotationValue = null;
        ushort? arrayNumValues = null;
        List<JvmElementValue>? arrayValues = null;
        ushort? enumConstNameIndex = null;

        switch (tag)
        {
            case 'B':
            case 'C':
            case 'D':
            case 'F':
            case 'I':
            case 'J':
            case 'S':
            case 'Z':
            case 's':
                constValueIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case 'e':
                typeNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                enumConstNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case 'c':
                classInfoIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case '@':
                annotationValue = DecodeAnnotation(data, ref offset);
                break;
            case '[':
                var numValues = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                arrayNumValues = numValues;
                arrayValues = new List<JvmElementValue>();
                for (var i = 0; i < numValues; i++)
                {
                    arrayValues.Add(DecodeElementValue(data, ref offset));
                }

                break;
        }

        return new JvmElementValue
        {
            Tag = (byte)tag,
            ConstValueIndex = constValueIndex,
            TypeNameIndex = typeNameIndex,
            ClassInfoIndex = classInfoIndex,
            AnnotationValue = annotationValue,
            ArrayNumValues = arrayNumValues,
            ArrayValues = arrayValues,
            EnumConstNameIndex = enumConstNameIndex
        };
    }

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
