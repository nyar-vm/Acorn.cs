using Acorn.Jvm.Data;
using Acorn.Jvm.Decode;
using Acorn.Jvm.Encode;

namespace Acorn.Tests.JvmTests;

public class JvmAttributeRoundTripTests
{
    /// <summary>
    ///     构建基础常量池（仅含 Utf8 和简单引用类型，无 Long/Double 避免索引偏移）
    /// </summary>
    private static (JvmConstant[] Pool, Dictionary<string, ushort> Names) BuildBasePool()
    {
        var pool = new List<JvmConstant>
        {
            new JvmConstantUtf8 { Value = "java/lang/Object" },       // 1
            new JvmConstantUtf8 { Value = "<init>" },                 // 2
            new JvmConstantUtf8 { Value = "()V" },                    // 3
            new JvmConstantUtf8 { Value = "Code" },                   // 4
            new JvmConstantUtf8 { Value = "LineNumberTable" },        // 5
            new JvmConstantUtf8 { Value = "LocalVariableTable" },     // 6
            new JvmConstantUtf8 { Value = "LocalVariableTypeTable" }, // 7
            new JvmConstantUtf8 { Value = "ConstantValue" },          // 8
            new JvmConstantUtf8 { Value = "SourceFile" },             // 9
            new JvmConstantUtf8 { Value = "Exceptions" },             // 10
            new JvmConstantUtf8 { Value = "InnerClasses" },           // 11
            new JvmConstantUtf8 { Value = "BootstrapMethods" },       // 12
            new JvmConstantUtf8 { Value = "Signature" },              // 13
            new JvmConstantUtf8 { Value = "Synthetic" },              // 14
            new JvmConstantUtf8 { Value = "Deprecated" },             // 15
            new JvmConstantUtf8 { Value = "EnclosingMethod" },        // 16
            new JvmConstantUtf8 { Value = "SourceDebugExtension" },   // 17
            new JvmConstantUtf8 { Value = "MethodParameters" },       // 18
            new JvmConstantUtf8 { Value = "StackMapTable" },          // 19
            new JvmConstantUtf8 { Value = "NestHost" },               // 20
            new JvmConstantUtf8 { Value = "NestMembers" },            // 21
            new JvmConstantUtf8 { Value = "Record" },                 // 22
            new JvmConstantUtf8 { Value = "PermittedSubclasses" },    // 23
            new JvmConstantUtf8 { Value = "RuntimeVisibleAnnotations" },       // 24
            new JvmConstantUtf8 { Value = "RuntimeInvisibleAnnotations" },     // 25
            new JvmConstantUtf8 { Value = "RuntimeVisibleParameterAnnotations" },  // 26
            new JvmConstantUtf8 { Value = "RuntimeInvisibleParameterAnnotations" }, // 27
            new JvmConstantUtf8 { Value = "AnnotationDefault" },      // 28
            new JvmConstantUtf8 { Value = "Module" },                 // 29
            new JvmConstantUtf8 { Value = "I" },                      // 30
            new JvmConstantUtf8 { Value = "testField" },              // 31
            new JvmConstantUtf8 { Value = "Ljava/lang/Object;" },     // 32
            new JvmConstantUtf8 { Value = "Ljava/lang/String;" },     // 33
            new JvmConstantUtf8 { Value = "Ljava/lang/Integer;" },    // 34
            new JvmConstantUtf8 { Value = "Ljava/util/List;" },       // 35
            new JvmConstantUtf8 { Value = "Lcom/example/Outer;" },    // 36
            new JvmConstantUtf8 { Value = "Lcom/example/Inner;" },    // 37
            new JvmConstantUtf8 { Value = "Lcom/example/Host;" },     // 38
            new JvmConstantUtf8 { Value = "Lcom/example/Member1;" },  // 39
            new JvmConstantUtf8 { Value = "Lcom/example/Member2;" },  // 40
            new JvmConstantUtf8 { Value = "Lcom/example/Sealed;" },   // 41
            new JvmConstantUtf8 { Value = "Lcom/example/Sub1;" },     // 42
            new JvmConstantUtf8 { Value = "Lcom/example/Sub2;" },     // 43
            new JvmConstantUtf8 { Value = "Lcom/example/Annotation;" }, // 44
            new JvmConstantUtf8 { Value = "value" },                  // 45
            new JvmConstantUtf8 { Value = "name" },                   // 46
            new JvmConstantUtf8 { Value = "com/example/module" },     // 47
            new JvmConstantUtf8 { Value = "1.0" },                    // 48
            new JvmConstantUtf8 { Value = "com/example/exported" },   // 49
            new JvmConstantUtf8 { Value = "com/example/opened" },     // 50
            new JvmConstantUtf8 { Value = "com/example/uses" },       // 51
            new JvmConstantUtf8 { Value = "com/example/provides" },   // 52
            new JvmConstantUtf8 { Value = "com/example/with" },       // 53
            new JvmConstantUtf8 { Value = "x" },                      // 54
            new JvmConstantUtf8 { Value = "y" },                      // 55
            new JvmConstantUtf8 { Value = "SourceFile.java" },        // 56
            new JvmConstantUtf8 { Value = "debug info" },             // 57
            new JvmConstantUtf8 { Value = "hello world" },            // 58
            new JvmConstantClass { NameIndex = 1 },                   // 59
            new JvmConstantNameAndType { NameIndex = 2, DescriptorIndex = 3 }, // 60
            new JvmConstantMethodref { ClassIndex = 59, NameAndTypeIndex = 60 }, // 61
            new JvmConstantInvokeDynamic { BootstrapMethodAttrIndex = 1, NameAndTypeIndex = 60 } // 62
        };

        var names = new Dictionary<string, ushort>();
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i] is JvmConstantUtf8 utf8)
            {
                names[utf8.Value] = (ushort)(i + 1);
            }
        }

        return (pool.ToArray(), names);
    }

    /// <summary>
    ///     构建最小 ClassFileData 辅助方法
    /// </summary>
    private static JvmClassFileData MakeClassFile(JvmConstant[] pool, Dictionary<string, ushort> names, List<JvmAttributeInfo> attributes)
    {
        return new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 61,
            ConstantPool = pool,
            AccessFlags = 0x0021,
            ThisClass = names["java/lang/Object"],
            SuperClass = names["java/lang/Object"],
            Interfaces = [],
            Fields = [],
            Methods = [],
            Attributes = attributes
        };
    }

    private static JvmClassFileData RoundTrip(JvmClassFileData original)
    {
        var encoder = new JvmEncoder();
        var bytes = encoder.Encode(original);
        var decoder = new JvmDecoder();
        return decoder.Decode(bytes);
    }

    /// <summary>
    ///     Signature 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_SignatureAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmSignatureAttribute
            {
                AttributeNameIndex = names["Signature"],
                AttributeLength = 2,
                SignatureIndex = names["Ljava/util/List;"]
            }
        ]);

        var decoded = RoundTrip(original);
        var sig = Assert.IsType<JvmSignatureAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["Ljava/util/List;"], sig.SignatureIndex);
    }

    /// <summary>
    ///     Synthetic 和 Deprecated 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_SyntheticAndDeprecatedAttributes()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmSyntheticAttribute { AttributeNameIndex = names["Synthetic"], AttributeLength = 0 },
            new JvmDeprecatedAttribute { AttributeNameIndex = names["Deprecated"], AttributeLength = 0 }
        ]);

        var decoded = RoundTrip(original);
        Assert.Equal(2, decoded.Attributes.Count);
        Assert.IsType<JvmSyntheticAttribute>(decoded.Attributes[0]);
        Assert.IsType<JvmDeprecatedAttribute>(decoded.Attributes[1]);
    }

    /// <summary>
    ///     EnclosingMethod 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_EnclosingMethodAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmEnclosingMethodAttribute
            {
                AttributeNameIndex = names["EnclosingMethod"],
                AttributeLength = 4,
                ClassIndex = names["java/lang/Object"],
                MethodIndex = names["<init>"]
            }
        ]);

        var decoded = RoundTrip(original);
        var em = Assert.IsType<JvmEnclosingMethodAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["java/lang/Object"], em.ClassIndex);
        Assert.Equal(names["<init>"], em.MethodIndex);
    }

    /// <summary>
    ///     SourceDebugExtension 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_SourceDebugExtensionAttribute()
    {
        var (pool, names) = BuildBasePool();
        var debugData = System.Text.Encoding.UTF8.GetBytes("SMAP\nTest.java\n*Scala\n*E\n");
        var original = MakeClassFile(pool, names,
        [
            new JvmSourceDebugExtensionAttribute
            {
                AttributeNameIndex = names["SourceDebugExtension"],
                AttributeLength = (uint)debugData.Length,
                DebugExtension = debugData
            }
        ]);

        var decoded = RoundTrip(original);
        var sde = Assert.IsType<JvmSourceDebugExtensionAttribute>(decoded.Attributes[0]);
        Assert.Equal(debugData, sde.DebugExtension);
    }

    /// <summary>
    ///     InnerClasses 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_InnerClassesAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmInnerClassesAttribute
            {
                AttributeNameIndex = names["InnerClasses"],
                AttributeLength = 18,
                NumberOfClasses = 2,
                Classes =
                [
                    new JvmInnerClassInfo
                    {
                        InnerClassInfoIndex = names["Lcom/example/Inner;"],
                        OuterClassInfoIndex = names["Lcom/example/Outer;"],
                        InnerNameIndex = names["testField"],
                        InnerClassAccessFlags = 0x0002
                    },
                    new JvmInnerClassInfo
                    {
                        InnerClassInfoIndex = names["Lcom/example/Host;"],
                        OuterClassInfoIndex = 0,
                        InnerNameIndex = 0,
                        InnerClassAccessFlags = 0x0400
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var ic = Assert.IsType<JvmInnerClassesAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)2, ic.NumberOfClasses);
        Assert.Equal(2, ic.Classes.Count);
        Assert.Equal(names["Lcom/example/Inner;"], ic.Classes[0].InnerClassInfoIndex);
        Assert.Equal(names["Lcom/example/Outer;"], ic.Classes[0].OuterClassInfoIndex);
        Assert.Equal((ushort)0x0002, ic.Classes[0].InnerClassAccessFlags);
        Assert.Equal(names["Lcom/example/Host;"], ic.Classes[1].InnerClassInfoIndex);
        Assert.Equal((ushort)0, ic.Classes[1].OuterClassInfoIndex);
    }

    /// <summary>
    ///     BootstrapMethods 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_BootstrapMethodsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmBootstrapMethodsAttribute
            {
                AttributeNameIndex = names["BootstrapMethods"],
                AttributeLength = 12,
                NumBootstrapMethods = 1,
                BootstrapMethods =
                [
                    new JvmBootstrapMethod
                    {
                        BootstrapMethodRef = names["java/lang/Object"],
                        NumBootstrapArguments = 2,
                        BootstrapArguments = [names["I"], names["testField"]]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var bm = Assert.IsType<JvmBootstrapMethodsAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)1, bm.NumBootstrapMethods);
        Assert.Single(bm.BootstrapMethods);
        Assert.Equal(names["java/lang/Object"], bm.BootstrapMethods[0].BootstrapMethodRef);
        Assert.Equal((ushort)2, bm.BootstrapMethods[0].NumBootstrapArguments);
        Assert.Equal(2, bm.BootstrapMethods[0].BootstrapArguments.Count);
    }

    /// <summary>
    ///     MethodParameters 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_MethodParametersAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmMethodParametersAttribute
            {
                AttributeNameIndex = names["MethodParameters"],
                AttributeLength = 9,
                ParameterCount = 2,
                Parameters =
                [
                    new JvmMethodParameterInfo { NameIndex = names["x"], AccessFlags = 0x0000 },
                    new JvmMethodParameterInfo { NameIndex = names["y"], AccessFlags = 0x0010 }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var mp = Assert.IsType<JvmMethodParametersAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)2, mp.ParameterCount);
        Assert.Equal(2, mp.Parameters.Count);
        Assert.Equal(names["x"], mp.Parameters[0].NameIndex);
        Assert.Equal((ushort)0x0000, mp.Parameters[0].AccessFlags);
        Assert.Equal(names["y"], mp.Parameters[1].NameIndex);
        Assert.Equal((ushort)0x0010, mp.Parameters[1].AccessFlags);
    }

    /// <summary>
    ///     NestHost 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_NestHostAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmNestHostAttribute
            {
                AttributeNameIndex = names["NestHost"],
                AttributeLength = 2,
                HostClassIndex = names["Lcom/example/Host;"]
            }
        ]);

        var decoded = RoundTrip(original);
        var nh = Assert.IsType<JvmNestHostAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["Lcom/example/Host;"], nh.HostClassIndex);
    }

    /// <summary>
    ///     NestMembers 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_NestMembersAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmNestMembersAttribute
            {
                AttributeNameIndex = names["NestMembers"],
                AttributeLength = 6,
                NumberOfClasses = 2,
                ClassIndexes = [names["Lcom/example/Member1;"], names["Lcom/example/Member2;"]]
            }
        ]);

        var decoded = RoundTrip(original);
        var nm = Assert.IsType<JvmNestMembersAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)2, nm.NumberOfClasses);
        Assert.Equal(2, nm.ClassIndexes.Count);
        Assert.Equal(names["Lcom/example/Member1;"], nm.ClassIndexes[0]);
        Assert.Equal(names["Lcom/example/Member2;"], nm.ClassIndexes[1]);
    }

    /// <summary>
    ///     PermittedSubclasses 属性往返测试（JVM 17+）
    /// </summary>
    [Fact]
    public void RoundTrip_PermittedSubclassesAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmPermittedSubclassesAttribute
            {
                AttributeNameIndex = names["PermittedSubclasses"],
                AttributeLength = 6,
                NumberOfClasses = 2,
                ClassIndexes = [names["Lcom/example/Sub1;"], names["Lcom/example/Sub2;"]]
            }
        ]);

        var decoded = RoundTrip(original);
        var ps = Assert.IsType<JvmPermittedSubclassesAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)2, ps.NumberOfClasses);
        Assert.Equal(2, ps.ClassIndexes.Count);
        Assert.Equal(names["Lcom/example/Sub1;"], ps.ClassIndexes[0]);
    }

    /// <summary>
    ///     Record 属性往返测试（JVM 16+）
    /// </summary>
    [Fact]
    public void RoundTrip_RecordAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmRecordAttribute
            {
                AttributeNameIndex = names["Record"],
                AttributeLength = 14,
                ComponentsCount = 2,
                Components =
                [
                    new JvmRecordComponentInfo
                    {
                        NameIndex = names["x"],
                        DescriptorIndex = names["I"],
                        Attributes = []
                    },
                    new JvmRecordComponentInfo
                    {
                        NameIndex = names["y"],
                        DescriptorIndex = names["Ljava/lang/String;"],
                        Attributes =
                        [
                            new JvmSignatureAttribute
                            {
                                AttributeNameIndex = names["Signature"],
                                AttributeLength = 2,
                                SignatureIndex = names["Ljava/util/List;"]
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var rec = Assert.IsType<JvmRecordAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)2, rec.ComponentsCount);
        Assert.Equal(2, rec.Components.Count);
        Assert.Equal(names["x"], rec.Components[0].NameIndex);
        Assert.Equal(names["I"], rec.Components[0].DescriptorIndex);
        Assert.Equal(names["y"], rec.Components[1].NameIndex);
        Assert.Equal(names["Ljava/lang/String;"], rec.Components[1].DescriptorIndex);
        Assert.Single(rec.Components[1].Attributes);
        var sig = Assert.IsType<JvmSignatureAttribute>(rec.Components[1].Attributes[0]);
        Assert.Equal(names["Ljava/util/List;"], sig.SignatureIndex);
    }

    /// <summary>
    ///     StackMapTable 属性往返测试（same_frame + same_locals_1_stack_item + full_frame）
    /// </summary>
    [Fact]
    public void RoundTrip_StackMapTableAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                AttributeNameIndex = names["StackMapTable"],
                AttributeLength = 20,
                NumberOfEntries = 3,
                Entries =
                [
                    new JvmSameFrame { FrameType = 0 },
                    new JvmSameLocals1StackItemFrame
                    {
                        FrameType = 64,
                        Stack = [new JvmVerificationTypeInfo { Tag = 1 }]
                    },
                    new JvmFullFrame
                    {
                        FrameType = 255,
                        OffsetDelta = 5,
                        NumberOfLocals = 2,
                        Locals =
                        [
                            new JvmVerificationTypeInfo { Tag = 1 },
                            new JvmVerificationTypeInfo { Tag = 7, CpoolIndex = names["java/lang/Object"] }
                        ],
                        NumberOfStackItems = 1,
                        Stack = [new JvmVerificationTypeInfo { Tag = 2 }]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)3, smt.NumberOfEntries);
        Assert.Equal(3, smt.Entries.Count);

        var same = Assert.IsType<JvmSameFrame>(smt.Entries[0]);
        Assert.Equal((byte)0, same.FrameType);

        var slsif = Assert.IsType<JvmSameLocals1StackItemFrame>(smt.Entries[1]);
        Assert.Equal((byte)64, slsif.FrameType);
        Assert.Single(slsif.Stack);
        Assert.Equal((byte)1, slsif.Stack[0].Tag);

        var full = Assert.IsType<JvmFullFrame>(smt.Entries[2]);
        Assert.Equal((byte)255, full.FrameType);
        Assert.Equal((ushort)5, full.OffsetDelta);
        Assert.Equal((ushort)2, full.NumberOfLocals);
        Assert.Equal((ushort)1, full.NumberOfStackItems);
        Assert.Equal((byte)7, full.Locals[1].Tag);
        Assert.Equal(names["java/lang/Object"], full.Locals[1].CpoolIndex);
    }

    /// <summary>
    ///     StackMapTable chop/append/same_frame_extended 帧类型往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_StackMapTable_SpecialFrameTypes()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                AttributeNameIndex = names["StackMapTable"],
                AttributeLength = 20,
                NumberOfEntries = 3,
                Entries =
                [
                    new JvmChopFrame { FrameType = 248, OffsetDelta = 10 },
                    new JvmSameFrameExtended { FrameType = 251, OffsetDelta = 20 },
                    new JvmAppendFrame
                    {
                        FrameType = 253,
                        OffsetDelta = 30,
                        Locals =
                        [
                            new JvmVerificationTypeInfo { Tag = 1 },
                            new JvmVerificationTypeInfo { Tag = 3 }
                        ]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.Attributes[0]);
        Assert.Equal(3, smt.Entries.Count);

        var chop = Assert.IsType<JvmChopFrame>(smt.Entries[0]);
        Assert.Equal((byte)248, chop.FrameType);
        Assert.Equal((ushort)10, chop.OffsetDelta);

        var sfe = Assert.IsType<JvmSameFrameExtended>(smt.Entries[1]);
        Assert.Equal((byte)251, sfe.FrameType);
        Assert.Equal((ushort)20, sfe.OffsetDelta);

        var append = Assert.IsType<JvmAppendFrame>(smt.Entries[2]);
        Assert.Equal((byte)253, append.FrameType);
        Assert.Equal((ushort)30, append.OffsetDelta);
        Assert.Equal(2, append.Locals.Count);
        Assert.Equal((byte)1, append.Locals[0].Tag);
        Assert.Equal((byte)3, append.Locals[1].Tag);
    }

    /// <summary>
    ///     RuntimeVisibleAnnotations 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_RuntimeVisibleAnnotationsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmRuntimeVisibleAnnotationsAttribute
            {
                AttributeNameIndex = names["RuntimeVisibleAnnotations"],
                AttributeLength = 12,
                NumAnnotations = 1,
                Annotations =
                [
                    new JvmAnnotation
                    {
                        TypeIndex = names["Lcom/example/Annotation;"],
                        NumElementValuePairs = 2,
                        ElementValuePairs =
                        [
                            new JvmElementValuePair
                            {
                                ElementNameIndex = names["value"],
                                Value = new JvmElementValue
                                {
                                    Tag = (byte)'s',
                                    ConstValueIndex = names["hello world"]
                                }
                            },
                            new JvmElementValuePair
                            {
                                ElementNameIndex = names["name"],
                                Value = new JvmElementValue
                                {
                                    Tag = (byte)'e',
                                    TypeNameIndex = names["Ljava/lang/String;"],
                                    EnumConstNameIndex = names["testField"]
                                }
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var rva = Assert.IsType<JvmRuntimeVisibleAnnotationsAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)1, rva.NumAnnotations);
        Assert.Single(rva.Annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], rva.Annotations[0].TypeIndex);
        Assert.Equal((ushort)2, rva.Annotations[0].NumElementValuePairs);
        Assert.Equal((byte)'s', rva.Annotations[0].ElementValuePairs[0].Value.Tag);
        Assert.Equal(names["hello world"], rva.Annotations[0].ElementValuePairs[0].Value.ConstValueIndex);
        Assert.Equal((byte)'e', rva.Annotations[0].ElementValuePairs[1].Value.Tag);
        Assert.Equal(names["Ljava/lang/String;"], rva.Annotations[0].ElementValuePairs[1].Value.TypeNameIndex);
    }

    /// <summary>
    ///     AnnotationDefault 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_AnnotationDefaultAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                AttributeNameIndex = names["AnnotationDefault"],
                AttributeLength = 3,
                DefaultValue = new JvmElementValue
                {
                    Tag = (byte)'I',
                    ConstValueIndex = names["I"]
                }
            }
        ]);

        var decoded = RoundTrip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)'I', ad.DefaultValue.Tag);
        Assert.Equal(names["I"], ad.DefaultValue.ConstValueIndex);
    }

    /// <summary>
    ///     AnnotationDefault 数组类型往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_AnnotationDefault_ArrayValue()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                AttributeNameIndex = names["AnnotationDefault"],
                AttributeLength = 8,
                DefaultValue = new JvmElementValue
                {
                    Tag = (byte)'[',
                    ArrayNumValues = 2,
                    ArrayValues =
                    [
                        new JvmElementValue { Tag = (byte)'I', ConstValueIndex = names["I"] },
                        new JvmElementValue { Tag = (byte)'s', ConstValueIndex = names["testField"] }
                    ]
                }
            }
        ]);

        var decoded = RoundTrip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)'[', ad.DefaultValue.Tag);
        Assert.Equal((ushort)2, ad.DefaultValue.ArrayNumValues);
        Assert.Equal(2, ad.DefaultValue.ArrayValues!.Count);
        Assert.Equal((byte)'I', ad.DefaultValue.ArrayValues[0].Tag);
        Assert.Equal((byte)'s', ad.DefaultValue.ArrayValues[1].Tag);
    }

    /// <summary>
    ///     Module 属性往返测试（JVM 9+）
    /// </summary>
    [Fact]
    public void RoundTrip_ModuleAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmModuleAttribute
            {
                AttributeNameIndex = names["Module"],
                AttributeLength = 40,
                ModuleNameIndex = names["com/example/module"],
                ModuleFlags = 0x0020,
                ModuleVersionIndex = names["1.0"],
                Requires =
                [
                    new JvmModuleRequire
                    {
                        RequiresIndex = names["com/example/module"],
                        RequiresFlags = 0x0040,
                        RequiresVersionIndex = names["1.0"]
                    }
                ],
                Exports =
                [
                    new JvmModuleExport
                    {
                        ExportsIndex = names["com/example/exported"],
                        ExportsFlags = 0x0000,
                        ExportsToIndex = [names["com/example/module"]]
                    }
                ],
                Opens =
                [
                    new JvmModuleOpen
                    {
                        OpensIndex = names["com/example/opened"],
                        OpensFlags = 0x0000,
                        OpensToIndex = []
                    }
                ],
                UsesIndex = [names["com/example/uses"]],
                Provides =
                [
                    new JvmModuleProvide
                    {
                        ProvidesIndex = names["com/example/provides"],
                        ProvidesWithIndex = [names["com/example/with"]]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var mod = Assert.IsType<JvmModuleAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["com/example/module"], mod.ModuleNameIndex);
        Assert.Equal((ushort)0x0020, mod.ModuleFlags);
        Assert.Equal(names["1.0"], mod.ModuleVersionIndex);
        Assert.Single(mod.Requires);
        Assert.Equal(names["com/example/module"], mod.Requires[0].RequiresIndex);
        Assert.Equal((ushort)0x0040, mod.Requires[0].RequiresFlags);
        Assert.Single(mod.Exports);
        Assert.Equal(names["com/example/exported"], mod.Exports[0].ExportsIndex);
        Assert.Single(mod.Exports[0].ExportsToIndex);
        Assert.Single(mod.Opens);
        Assert.Single(mod.UsesIndex);
        Assert.Single(mod.Provides);
        Assert.Single(mod.Provides[0].ProvidesWithIndex);
    }

    /// <summary>
    ///     Dynamic/Module/Package 常量池往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_DynamicModulePackageConstants()
    {
        var pool = new JvmConstant[]
        {
            new JvmConstantUtf8 { Value = "java/lang/Object" },  // 1
            new JvmConstantUtf8 { Value = "<init>" },            // 2
            new JvmConstantUtf8 { Value = "()V" },               // 3
            new JvmConstantUtf8 { Value = "com/example/module" },// 4
            new JvmConstantUtf8 { Value = "com/example/pkg" },   // 5
            new JvmConstantClass { NameIndex = 1 },              // 6
            new JvmConstantNameAndType { NameIndex = 2, DescriptorIndex = 3 }, // 7
            new JvmConstantDynamic { BootstrapMethodAttrIndex = 1, NameAndTypeIndex = 7 }, // 8
            new JvmConstantModule { NameIndex = 4 },             // 9
            new JvmConstantPackage { NameIndex = 5 }             // 10
        };

        var original = new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 55,
            ConstantPool = pool,
            AccessFlags = 0x0021,
            ThisClass = 6,
            SuperClass = 6,
            Interfaces = [],
            Fields = [],
            Methods = [],
            Attributes = []
        };

        var decoded = RoundTrip(original);
        Assert.Equal(pool.Length, decoded.ConstantPool.Count);

        var dyn = Assert.IsType<JvmConstantDynamic>(decoded.ConstantPool[7]);
        Assert.Equal((ushort)1, dyn.BootstrapMethodAttrIndex);
        Assert.Equal((ushort)7, dyn.NameAndTypeIndex);

        var mod = Assert.IsType<JvmConstantModule>(decoded.ConstantPool[8]);
        Assert.Equal((ushort)4, mod.NameIndex);

        var pkg = Assert.IsType<JvmConstantPackage>(decoded.ConstantPool[9]);
        Assert.Equal((ushort)5, pkg.NameIndex);
    }

    /// <summary>
    ///     Long/Double 常量池往返测试（验证占位索引跳过）
    /// </summary>
    [Fact]
    public void RoundTrip_LongDoubleConstantPool()
    {
        var pool = new JvmConstant[]
        {
            new JvmConstantUtf8 { Value = "test" },              // 1
            new JvmConstantLong { Value = 0x123456789ABCDEF0L }, // 2 (占 2,3)
            new JvmConstantDouble { Value = 2.718281828 },       // 4 (占 4,5)
            new JvmConstantUtf8 { Value = "after" }              // 6
        };

        var original = new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 61,
            ConstantPool = pool,
            AccessFlags = 0,
            ThisClass = 0,
            SuperClass = 0,
            Interfaces = [],
            Fields = [],
            Methods = [],
            Attributes = []
        };

        var decoded = RoundTrip(original);
        Assert.Equal(4, decoded.ConstantPool.Count);
        var l = Assert.IsType<JvmConstantLong>(decoded.ConstantPool[1]);
        Assert.Equal(0x123456789ABCDEF0L, l.Value);
        var d = Assert.IsType<JvmConstantDouble>(decoded.ConstantPool[2]);
        Assert.Equal(2.718281828, d.Value, 9);
        var after = Assert.IsType<JvmConstantUtf8>(decoded.ConstantPool[3]);
        Assert.Equal("after", after.Value);
    }

    /// <summary>
    ///     LocalVariableTypeTable 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_LocalVariableTypeTableAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmLocalVariableTypeTableAttribute
            {
                AttributeNameIndex = names["LocalVariableTypeTable"],
                AttributeLength = 12,
                LocalVariableTypeTableLength = 1,
                LocalVariableTypeTable =
                [
                    new JvmLocalVariableTypeEntry
                    {
                        StartPc = 0,
                        Length = 10,
                        NameIndex = names["x"],
                        SignatureIndex = names["Ljava/util/List;"],
                        Index = 1
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var lvtt = Assert.IsType<JvmLocalVariableTypeTableAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)1, lvtt.LocalVariableTypeTableLength);
        Assert.Single(lvtt.LocalVariableTypeTable);
        Assert.Equal((ushort)0, lvtt.LocalVariableTypeTable[0].StartPc);
        Assert.Equal((ushort)10, lvtt.LocalVariableTypeTable[0].Length);
        Assert.Equal(names["x"], lvtt.LocalVariableTypeTable[0].NameIndex);
        Assert.Equal(names["Ljava/util/List;"], lvtt.LocalVariableTypeTable[0].SignatureIndex);
        Assert.Equal((ushort)1, lvtt.LocalVariableTypeTable[0].Index);
    }

    /// <summary>
    ///     RuntimeVisibleParameterAnnotations 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_RuntimeVisibleParameterAnnotationsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmRuntimeVisibleParameterAnnotationsAttribute
            {
                AttributeNameIndex = names["RuntimeVisibleParameterAnnotations"],
                AttributeLength = 10,
                NumParameters = 1,
                ParameterAnnotations =
                [
                    new JvmParameterAnnotations
                    {
                        NumAnnotations = 1,
                        Annotations =
                        [
                            new JvmAnnotation
                            {
                                TypeIndex = names["Lcom/example/Annotation;"],
                                NumElementValuePairs = 0,
                                ElementValuePairs = []
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var rvpa = Assert.IsType<JvmRuntimeVisibleParameterAnnotationsAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)1, rvpa.NumParameters);
        Assert.Single(rvpa.ParameterAnnotations);
        Assert.Equal((ushort)1, rvpa.ParameterAnnotations[0].NumAnnotations);
        Assert.Single(rvpa.ParameterAnnotations[0].Annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], rvpa.ParameterAnnotations[0].Annotations[0].TypeIndex);
    }

    /// <summary>
    ///     嵌套注解（AnnotationDefault 包含嵌套 @ 注解）往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_NestedAnnotationInAnnotationDefault()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                AttributeNameIndex = names["AnnotationDefault"],
                AttributeLength = 8,
                DefaultValue = new JvmElementValue
                {
                    Tag = (byte)'@',
                    AnnotationValue = new JvmAnnotation
                    {
                        TypeIndex = names["Lcom/example/Annotation;"],
                        NumElementValuePairs = 1,
                        ElementValuePairs =
                        [
                            new JvmElementValuePair
                            {
                                ElementNameIndex = names["value"],
                                Value = new JvmElementValue
                                {
                                    Tag = (byte)'c',
                                    ClassInfoIndex = names["Ljava/lang/String;"]
                                }
                            }
                        ]
                    }
                }
            }
        ]);

        var decoded = RoundTrip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)'@', ad.DefaultValue.Tag);
        Assert.NotNull(ad.DefaultValue.AnnotationValue);
        Assert.Equal(names["Lcom/example/Annotation;"], ad.DefaultValue.AnnotationValue.TypeIndex);
        Assert.Equal((ushort)1, ad.DefaultValue.AnnotationValue.NumElementValuePairs);
        Assert.Equal((byte)'c', ad.DefaultValue.AnnotationValue.ElementValuePairs[0].Value.Tag);
        Assert.Equal(names["Ljava/lang/String;"], ad.DefaultValue.AnnotationValue.ElementValuePairs[0].Value.ClassInfoIndex);
    }

    /// <summary>
    ///     Code 属性包含 LineNumberTable + LocalVariableTable 子属性的往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_CodeAttribute_WithSubAttributes()
    {
        var (pool, names) = BuildBasePool();
        var original = new JvmClassFileData
        {
            Magic = JvmConstants.Magic,
            MinorVersion = 0,
            MajorVersion = 61,
            ConstantPool = pool,
            AccessFlags = 0x0021,
            ThisClass = names["java/lang/Object"],
            SuperClass = names["java/lang/Object"],
            Interfaces = [],
            Fields = [],
            Methods =
            [
                new JvmMethodInfo
                {
                    AccessFlags = 0x0001,
                    NameIndex = names["<init>"],
                    DescriptorIndex = names["()V"],
                    Attributes =
                    [
                        new JvmCodeAttribute
                        {
                            AttributeNameIndex = names["Code"],
                            AttributeLength = 40,
                            MaxStack = 1,
                            MaxLocals = 1,
                            CodeLength = 1,
                            Code = [0xB1],
                            ExceptionTableLength = 0,
                            ExceptionTable = [],
                            AttributesCount = 2,
                            Attributes =
                            [
                                new JvmLineNumberTableAttribute
                                {
                                    AttributeNameIndex = names["LineNumberTable"],
                                    AttributeLength = 6,
                                    LineNumberTableLength = 1,
                                    LineNumberTable =
                                    [
                                        new JvmLineNumberEntry { StartPc = 0, LineNumber = 42 }
                                    ]
                                },
                                new JvmLocalVariableTableAttribute
                                {
                                    AttributeNameIndex = names["LocalVariableTable"],
                                    AttributeLength = 12,
                                    LocalVariableTableLength = 1,
                                    LocalVariableTable =
                                    [
                                        new JvmLocalVariableEntry
                                        {
                                            StartPc = 0,
                                            Length = 5,
                                            NameIndex = names["x"],
                                            DescriptorIndex = names["I"],
                                            Index = 0
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ],
            Attributes = []
        };

        var decoded = RoundTrip(original);
        Assert.Single(decoded.Methods);
        var code = Assert.IsType<JvmCodeAttribute>(decoded.Methods[0].Attributes[0]);
        Assert.Equal((ushort)1, code.MaxStack);
        Assert.Equal((ushort)1, code.MaxLocals);
        Assert.Single(code.Code);
        Assert.Equal((byte)0xB1, code.Code[0]);
        Assert.Equal(2, code.Attributes.Count);

        var lnt = Assert.IsType<JvmLineNumberTableAttribute>(code.Attributes[0]);
        Assert.Single(lnt.LineNumberTable);
        Assert.Equal((ushort)0, lnt.LineNumberTable[0].StartPc);
        Assert.Equal((ushort)42, lnt.LineNumberTable[0].LineNumber);

        var lvt = Assert.IsType<JvmLocalVariableTableAttribute>(code.Attributes[1]);
        Assert.Single(lvt.LocalVariableTable);
        Assert.Equal(names["x"], lvt.LocalVariableTable[0].NameIndex);
        Assert.Equal(names["I"], lvt.LocalVariableTable[0].DescriptorIndex);
    }

    /// <summary>
    ///     RuntimeInvisibleAnnotations 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_RuntimeInvisibleAnnotationsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmRuntimeInvisibleAnnotationsAttribute
            {
                AttributeNameIndex = names["RuntimeInvisibleAnnotations"],
                AttributeLength = 6,
                NumAnnotations = 1,
                Annotations =
                [
                    new JvmAnnotation
                    {
                        TypeIndex = names["Lcom/example/Annotation;"],
                        NumElementValuePairs = 0,
                        ElementValuePairs = []
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var ria = Assert.IsType<JvmRuntimeInvisibleAnnotationsAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)1, ria.NumAnnotations);
        Assert.Single(ria.Annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], ria.Annotations[0].TypeIndex);
    }

    /// <summary>
    ///     RuntimeInvisibleParameterAnnotations 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_RuntimeInvisibleParameterAnnotationsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmRuntimeInvisibleParameterAnnotationsAttribute
            {
                AttributeNameIndex = names["RuntimeInvisibleParameterAnnotations"],
                AttributeLength = 4,
                NumParameters = 1,
                ParameterAnnotations =
                [
                    new JvmParameterAnnotations
                    {
                        NumAnnotations = 0,
                        Annotations = []
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var ripa = Assert.IsType<JvmRuntimeInvisibleParameterAnnotationsAttribute>(decoded.Attributes[0]);
        Assert.Equal((byte)1, ripa.NumParameters);
        Assert.Single(ripa.ParameterAnnotations);
        Assert.Equal((ushort)0, ripa.ParameterAnnotations[0].NumAnnotations);
    }

    /// <summary>
    ///     Exceptions 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_ExceptionsAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmExceptionsAttribute
            {
                AttributeNameIndex = names["Exceptions"],
                AttributeLength = 6,
                NumberOfExceptions = 2,
                ExceptionIndexTable = [names["Lcom/example/Inner;"], names["Lcom/example/Host;"]]
            }
        ]);

        var decoded = RoundTrip(original);
        var exc = Assert.IsType<JvmExceptionsAttribute>(decoded.Attributes[0]);
        Assert.Equal((ushort)2, exc.NumberOfExceptions);
        Assert.Equal(2, exc.ExceptionIndexTable.Count);
        Assert.Equal(names["Lcom/example/Inner;"], exc.ExceptionIndexTable[0]);
        Assert.Equal(names["Lcom/example/Host;"], exc.ExceptionIndexTable[1]);
    }

    /// <summary>
    ///     SourceFile 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_SourceFileAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmSourceFileAttribute
            {
                AttributeNameIndex = names["SourceFile"],
                AttributeLength = 2,
                SourceFileIndex = names["SourceFile.java"]
            }
        ]);

        var decoded = RoundTrip(original);
        var sf = Assert.IsType<JvmSourceFileAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["SourceFile.java"], sf.SourceFileIndex);
    }

    /// <summary>
    ///     ConstantValue 属性往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_ConstantValueAttribute()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmConstantValueAttribute
            {
                AttributeNameIndex = names["ConstantValue"],
                AttributeLength = 2,
                ConstantValueIndex = names["I"]
            }
        ]);

        var decoded = RoundTrip(original);
        var cv = Assert.IsType<JvmConstantValueAttribute>(decoded.Attributes[0]);
        Assert.Equal(names["I"], cv.ConstantValueIndex);
    }

    /// <summary>
    ///     StackMapTable Uninitialized verification type info 往返测试
    /// </summary>
    [Fact]
    public void RoundTrip_StackMapTable_UninitializedVti()
    {
        var (pool, names) = BuildBasePool();
        var original = MakeClassFile(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                AttributeNameIndex = names["StackMapTable"],
                AttributeLength = 10,
                NumberOfEntries = 1,
                Entries =
                [
                    new JvmSameLocals1StackItemFrame
                    {
                        FrameType = 64,
                        Stack = [new JvmVerificationTypeInfo { Tag = 8, Offset = 42 }]
                    }
                ]
            }
        ]);

        var decoded = RoundTrip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.Attributes[0]);
        var slsif = Assert.IsType<JvmSameLocals1StackItemFrame>(smt.Entries[0]);
        Assert.Equal((byte)8, slsif.Stack[0].Tag);
        Assert.Equal((ushort)42, slsif.Stack[0].Offset);
    }
}
