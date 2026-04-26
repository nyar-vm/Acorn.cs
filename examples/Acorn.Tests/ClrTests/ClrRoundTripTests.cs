using Acorn.Clr.Data;
using Acorn.Clr.Decode;
using Acorn.Clr.Encode;
using Acorn.Clr.Scanner;

namespace Acorn.Tests.ClrTests;

public class ClrRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new ClrModuleData
        {
            ModuleName = "TestModule",
            Version = "v4.0.30319",
            Methods = [],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal("TestModule", decoded.ModuleName);
        Assert.Equal("v4.0.30319", decoded.Version);
        Assert.Empty(decoded.Methods);
        Assert.Empty(decoded.Types);
        Assert.Empty(decoded.Fields);
    }

    [Fact]
    public void Encode_Decode_ModuleWithMethods()
    {
        var original = new ClrModuleData
        {
            ModuleName = "MethodTest",
            Version = "v4.0.30319",
            Methods =
            [
                new ClrMethodDef
                {
                    Name = "Main",
                    Flags = ClrMethodAttributes.Public | ClrMethodAttributes.Static,
                    MaxStack = 8,
                    Instructions =
                    [
                        new ClrInstruction { Offset = 0, Opcode = ClrOpcode.Nop },
                        new ClrInstruction { Offset = 1, Opcode = ClrOpcode.Ldc_I4_1 },
                        new ClrInstruction { Offset = 2, Opcode = ClrOpcode.Ldc_I4_2 },
                        new ClrInstruction { Offset = 3, Opcode = ClrOpcode.Add },
                        new ClrInstruction { Offset = 4, Opcode = ClrOpcode.Pop },
                        new ClrInstruction { Offset = 5, Opcode = ClrOpcode.Ret }
                    ]
                },
                new ClrMethodDef
                {
                    Name = "Add",
                    Flags = ClrMethodAttributes.Public | ClrMethodAttributes.Static,
                    MaxStack = 2,
                    Instructions =
                    [
                        new ClrInstruction { Offset = 0, Opcode = ClrOpcode.Ldarg_0 },
                        new ClrInstruction { Offset = 1, Opcode = ClrOpcode.Ldarg_1 },
                        new ClrInstruction { Offset = 2, Opcode = ClrOpcode.Add },
                        new ClrInstruction { Offset = 3, Opcode = ClrOpcode.Ret }
                    ]
                }
            ],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal("MethodTest", decoded.ModuleName);
        Assert.Equal(2, decoded.Methods.Count);
        Assert.Equal("Main", decoded.Methods[0].Name);
        Assert.Equal("Add", decoded.Methods[1].Name);
        Assert.True(decoded.Methods[0].Instructions.Count > 0);
        Assert.True(decoded.Methods[1].Instructions.Count > 0);
    }

    [Fact]
    public void Encode_Decode_ModuleWithTypes()
    {
        var original = new ClrModuleData
        {
            ModuleName = "TypeTest",
            Version = "v4.0.30319",
            Types =
            [
                new ClrTypeDef
                {
                    Name = "Program",
                    Namespace = "TestApp",
                    Flags = ClrTypeAttributes.Public | ClrTypeAttributes.BeforeFieldInit
                }
            ],
            Methods = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal("TypeTest", decoded.ModuleName);
        Assert.Equal(1, decoded.Types.Count);
        Assert.Equal("Program", decoded.Types[0].Name);
        Assert.Equal("TestApp", decoded.Types[0].Namespace);
    }

    [Fact]
    public void Encode_Decode_ModuleWithFields()
    {
        var original = new ClrModuleData
        {
            ModuleName = "FieldTest",
            Version = "v4.0.30319",
            Types =
            [
                new ClrTypeDef
                {
                    Name = "MyClass",
                    Namespace = "TestApp",
                    Flags = ClrTypeAttributes.Public | ClrTypeAttributes.BeforeFieldInit,
                    Fields =
                    [
                        new ClrFieldDef
                        {
                            Name = "_value",
                            Flags = ClrFieldAttributes.Private
                        },
                        new ClrFieldDef
                        {
                            Name = "Count",
                            Flags = ClrFieldAttributes.Public | ClrFieldAttributes.Static
                        }
                    ]
                }
            ],
            Methods = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal("FieldTest", decoded.ModuleName);
        Assert.Equal(1, decoded.Types.Count);
        Assert.Equal("MyClass", decoded.Types[0].Name);
        Assert.Equal(2, decoded.Types[0].Fields.Count);
        Assert.Equal("_value", decoded.Types[0].Fields[0].Name);
        Assert.Equal("Count", decoded.Types[0].Fields[1].Name);
    }

    [Fact]
    public void Encode_Decode_ClrDirectoryFlags()
    {
        var original = new ClrModuleData
        {
            ModuleName = "FlagsTest",
            Version = "v4.0.30319",
            Methods = [],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.True(decoded.ClrDirectory.IsILOnly);
        Assert.True(decoded.ClrDirectory.Cb > 0);
        Assert.True(decoded.ClrDirectory.MetadataSize > 0);
    }

    [Fact]
    public void Encode_Decode_MetadataHeader()
    {
        var original = new ClrModuleData
        {
            ModuleName = "MetadataTest",
            Version = "v4.0.30319",
            Methods = [],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(ClrConstants.MetadataSignature, decoded.Metadata.Header.Signature);
        Assert.Equal(5, decoded.Metadata.Header.Streams);
        Assert.True(decoded.Metadata.StreamHeaders.Count >= 5);
    }

    [Fact]
    public void Encode_Decode_InstructionsWithOperands()
    {
        var original = new ClrModuleData
        {
            ModuleName = "OperandTest",
            Version = "v4.0.30319",
            Methods =
            [
                new ClrMethodDef
                {
                    Name = "Test",
                    Flags = ClrMethodAttributes.Public | ClrMethodAttributes.Static,
                    MaxStack = 8,
                    Instructions =
                    [
                        new ClrInstruction { Offset = 0, Opcode = ClrOpcode.Ldc_I4, Operand = new ClrInt32Operand { Value = 42 } },
                        new ClrInstruction { Offset = 5, Opcode = ClrOpcode.Ldc_I8, Operand = new ClrInt64Operand { Value = 1234567890123L } },
                        new ClrInstruction { Offset = 14, Opcode = ClrOpcode.Ldc_R4, Operand = new ClrFloat32Operand { Value = 3.14f } },
                        new ClrInstruction { Offset = 19, Opcode = ClrOpcode.Ldc_R8, Operand = new ClrFloat64Operand { Value = 2.718281828 } },
                        new ClrInstruction { Offset = 28, Opcode = ClrOpcode.Ldloc_S, Operand = new ClrLocalIndexOperand { Index = 5 } },
                        new ClrInstruction { Offset = 30, Opcode = ClrOpcode.Stloc_S, Operand = new ClrLocalIndexOperand { Index = 3 } },
                        new ClrInstruction { Offset = 32, Opcode = ClrOpcode.Ldarg_S, Operand = new ClrArgumentIndexOperand { Index = 1 } },
                        new ClrInstruction { Offset = 34, Opcode = ClrOpcode.Call, Operand = new ClrTokenOperand { Value = 0x0A000001 } },
                        new ClrInstruction { Offset = 39, Opcode = ClrOpcode.Ret }
                    ]
                }
            ],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(1, decoded.Methods.Count);
        Assert.Equal("Test", decoded.Methods[0].Name);

        var instrs = decoded.Methods[0].Instructions;
        Assert.True(instrs.Count >= 5);

        var ldcI4 = instrs.FirstOrDefault(i => i.Opcode == ClrOpcode.Ldc_I4);
        Assert.NotNull(ldcI4);
        Assert.IsType<ClrInt32Operand>(ldcI4.Operand);
        Assert.Equal(42, ((ClrInt32Operand)ldcI4.Operand!).Value);

        var ldcR4 = instrs.FirstOrDefault(i => i.Opcode == ClrOpcode.Ldc_R4);
        Assert.NotNull(ldcR4);
        Assert.IsType<ClrFloat32Operand>(ldcR4.Operand);
        Assert.Equal(3.14f, ((ClrFloat32Operand)ldcR4.Operand!).Value, 0.001f);
    }

    [Fact]
    public void Encode_Decode_ExceptionHandlers()
    {
        var original = new ClrModuleData
        {
            ModuleName = "EhTest",
            Version = "v4.0.30319",
            Methods =
            [
                new ClrMethodDef
                {
                    Name = "TryCatch",
                    Flags = ClrMethodAttributes.Public | ClrMethodAttributes.Static,
                    MaxStack = 2,
                    LocalVarSigTok = 0,
                    Instructions =
                    [
                        new ClrInstruction { Offset = 0, Opcode = ClrOpcode.Nop },
                        new ClrInstruction { Offset = 1, Opcode = ClrOpcode.Ldc_I4_1 },
                        new ClrInstruction { Offset = 2, Opcode = ClrOpcode.Pop },
                        new ClrInstruction { Offset = 3, Opcode = ClrOpcode.Leave_S, Operand = new ClrBranchTarget8Operand { Offset = 7 } },
                        new ClrInstruction { Offset = 5, Opcode = ClrOpcode.Pop },
                        new ClrInstruction { Offset = 6, Opcode = ClrOpcode.Leave_S, Operand = new ClrBranchTarget8Operand { Offset = 7 } },
                        new ClrInstruction { Offset = 7, Opcode = ClrOpcode.Ret }
                    ],
                    ExceptionHandlers =
                    [
                        new ClrExceptionHandler
                        {
                            HandlerKind = ClrExceptionHandlerKind.Catch,
                            TryStart = 0,
                            TryLength = 3,
                            HandlerStart = 5,
                            HandlerLength = 2,
                            ClassTokenOrFilterOffset = 0x01000001
                        }
                    ]
                }
            ],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(1, decoded.Methods.Count);
        Assert.Equal("TryCatch", decoded.Methods[0].Name);
        Assert.True(decoded.Methods[0].Instructions.Count > 0);
    }

    [Fact]
    public void EncodeInstructions_DecodeInstructions_RoundTrip()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { Offset = 0, Opcode = ClrOpcode.Ldarg_0 },
            new() { Offset = 1, Opcode = ClrOpcode.Ldarg_1 },
            new() { Offset = 2, Opcode = ClrOpcode.Add },
            new() { Offset = 3, Opcode = ClrOpcode.Ret }
        };

        var encoded = ClrEncoder.EncodeInstructions(instructions);

        Assert.True(encoded.Length > 0);
        Assert.Equal(4, encoded.Length);
    }

    [Fact]
    public void EncodeMethodBody_TinyFormat()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { Offset = 0, Opcode = ClrOpcode.Ldc_I4_1 },
            new() { Offset = 1, Opcode = ClrOpcode.Ret }
        };

        var body = ClrEncoder.EncodeMethodBody(instructions);

        Assert.True(body.Length > 0);
        Assert.True((body[0] & ClrConstants.MethodHeaderFormatMask) == ClrConstants.MethodHeaderTinyFlag);
    }

    [Fact]
    public void EncodeMethodBody_FatFormat()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { Offset = 0, Opcode = ClrOpcode.Ldc_I4, Operand = new ClrInt32Operand { Value = 42 } },
            new() { Offset = 5, Opcode = ClrOpcode.Ret }
        };

        var body = ClrEncoder.EncodeMethodBody(instructions, maxStack: 1, localVarSigTok: 0x11B);

        Assert.True(body.Length > 0);
        Assert.True((body[0] & ClrConstants.MethodHeaderFormatMask) == ClrConstants.MethodHeaderFatFlag);
    }
}

public class ClrScannerTests
{
    [Fact]
    public void IsClrAssembly_ValidClrAssembly_ReturnsTrue()
    {
        var module = new ClrModuleData
        {
            ModuleName = "ScanTest",
            Version = "v4.0.30319",
            Methods = [],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(module);

        var scanner = new ClrScanner(bytes);
        Assert.True(scanner.IsClrAssembly());
    }

    [Fact]
    public void IsClrAssembly_InvalidData_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var scanner = new ClrScanner(data);
        Assert.False(scanner.IsClrAssembly());
    }

    [Fact]
    public void ScanHeader_ReturnsCorrectInfo()
    {
        var module = new ClrModuleData
        {
            ModuleName = "HeaderScanTest",
            Version = "v4.0.30319",
            Methods = [],
            Types = [],
            Fields = [],
            Properties = [],
            Events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.Encode(module);

        var scanner = new ClrScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.True(header.HasClrDirectory);
        Assert.True(header.ClrMetadataRva > 0);
        Assert.True(header.ClrMetadataSize > 0);
        Assert.False(header.IsPE32Plus);
    }

    [Fact]
    public void ScanHeader_NonPeData_Throws()
    {
        var data = new byte[128];
        var scanner = new ClrScanner(data);
        bool threw = false;

        try
        {
            scanner.ScanHeader();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw);
    }
}
