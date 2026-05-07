using Acorn.Wasm.Data;
using Acorn.Wasm.Decode;
using Acorn.Wasm.Encode;

namespace Acorn.Tests.WasmTests;

public class WasmRoundTripTests
{
    [Fact]
    public void Encode_MinimalModule_HeaderBytes()
    {
        var original = new WasmModuleData();
        var bytes = WasmEncoder.EncodeModule(original);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x61, bytes[1]);
        Assert.Equal(0x73, bytes[2]);
        Assert.Equal(0x6D, bytes[3]);
        Assert.Equal(1u, BitConverter.ToUInt32(bytes, 4));
    }

    [Fact]
    public void Decode_MinimalModule_FromRawBytes()
    {
        var rawWasm = new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        var decoded = WasmDecoder.DecodeModule(rawWasm);
        Assert.Equal(1u, decoded.Version);
    }

    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new WasmModuleData();

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Empty(decoded.Types);
        Assert.Empty(decoded.Imports);
        Assert.Empty(decoded.FunctionTypeIndices);
        Assert.Empty(decoded.Tables);
        Assert.Empty(decoded.Memories);
        Assert.Empty(decoded.Globals);
        Assert.Empty(decoded.Exports);
        Assert.Null(decoded.StartFunctionIndex);
        Assert.Empty(decoded.Elements);
        Assert.Empty(decoded.Codes);
        Assert.Empty(decoded.DataSegments);
    }

    [Fact]
    public void Encode_Decode_ModuleWithTypesAndFunctions()
    {
        var types = new WasmFunctionType[]
        {
            new() { Parameters = [], Results = [] },
            new() { Parameters = [WasmValueType.Int32, WasmValueType.Int32], Results = [WasmValueType.Int32] }
        };

        var imports = new WasmImport[]
        {
            new()
            {
                Module = "env",
                Field = "memory",
                Descriptor = new WasmImportDescriptor
                {
                    Kind = WasmExternalKind.Memory,
                    MemoryType = new WasmMemoryType { Limits = new WasmLimits { Minimum = 1 } }
                }
            }
        };

        var exports = new WasmExport[]
        {
            new() { Name = "main", Kind = WasmExternalKind.Function, Index = 0 }
        };

        var codes = new WasmCode[]
        {
            new() { Body = [0x00, 0x0B] }
        };

        var original = new WasmModuleData
        {
            Types = types,
            Imports = imports,
            FunctionTypeIndices = [0],
            Exports = exports,
            Codes = codes
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(2, decoded.Types.Count);
        Assert.Empty(decoded.Types[0].Parameters);
        Assert.Empty(decoded.Types[0].Results);
        Assert.Equal(2, decoded.Types[1].Parameters.Count);
        Assert.Equal(WasmValueType.Int32, decoded.Types[1].Parameters[0]);
        Assert.Single(decoded.Types[1].Results);
        Assert.Equal(WasmValueType.Int32, decoded.Types[1].Results[0]);

        Assert.Single(decoded.Imports);
        Assert.Equal("env", decoded.Imports[0].Module);
        Assert.Equal("memory", decoded.Imports[0].Field);

        Assert.Single(decoded.Exports);
        Assert.Equal("main", decoded.Exports[0].Name);

        Assert.Single(decoded.Codes);
        Assert.Equal(codes[0].Body, decoded.Codes[0].Body);
    }

    [Fact]
    public void Encode_Decode_ModuleWithMemory()
    {
        var original = new WasmModuleData
        {
            Memories = [new WasmMemory { Type = new WasmMemoryType { Limits = new WasmLimits { Minimum = 1, Maximum = 256 } } }]
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Single(decoded.Memories);
        Assert.Equal((uint)1, decoded.Memories[0].Type.Limits.Minimum);
        Assert.Equal((uint)256, decoded.Memories[0].Type.Limits.Maximum);
    }

    [Fact]
    public void Encode_Decode_ModuleWithGlobals()
    {
        var original = new WasmModuleData
        {
            Globals = [new WasmGlobal
            {
                Type = new WasmGlobalType { ValueType = WasmValueType.Int32, Mutable = true },
                InitExpression = [0x41, 0x2A, 0x0B]
            }]
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Single(decoded.Globals);
        Assert.Equal(WasmValueType.Int32, decoded.Globals[0].Type.ValueType);
        Assert.True(decoded.Globals[0].Type.Mutable);
        Assert.Equal(new byte[] { 0x41, 0x2A, 0x0B }, decoded.Globals[0].InitExpression);
    }

    [Fact]
    public void Encode_Decode_ModuleWithStartFunction()
    {
        var original = new WasmModuleData
        {
            Types = [new WasmFunctionType()],
            FunctionTypeIndices = [0],
            StartFunctionIndex = 0,
            Codes = [new WasmCode { Body = [0x0B] }]
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Equal((uint)0, decoded.StartFunctionIndex);
    }

    [Fact]
    public void Encode_Decode_ModuleWithCustomSection()
    {
        var original = new WasmModuleData
        {
            CustomSections = [new WasmCustomSection { Name = "producers", Data = [0x01, 0x02, 0x03] }]
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Single(decoded.CustomSections);
        Assert.Equal("producers", decoded.CustomSections[0].Name);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, decoded.CustomSections[0].Data);
    }

    [Fact]
    public void Encode_Decode_ModuleWithDataSegments()
    {
        var original = new WasmModuleData
        {
            Memories = [new WasmMemory { Type = new WasmMemoryType { Limits = new WasmLimits { Minimum = 1 } } }],
            DataSegments = [new WasmData
            {
                MemoryIndex = 0,
                OffsetExpression = [0x41, 0x00, 0x0B],
                Initializer = [0xDE, 0xAD, 0xBE, 0xEF]
            }]
        };

        var bytes = WasmEncoder.EncodeModule(original);
        var decoded = WasmDecoder.DecodeModule(bytes);

        Assert.Single(decoded.DataSegments);
        Assert.Equal((uint)0, decoded.DataSegments[0].MemoryIndex);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, decoded.DataSegments[0].Initializer);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00 };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.DecodeModule(data));
    }

    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[3];

        Assert.ThrowsAny<Exception>(() => WasmDecoder.DecodeModule(data));
    }
}
