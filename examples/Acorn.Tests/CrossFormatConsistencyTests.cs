using System;
using Acorn.Frame;
using Acorn.Wasm.Data;
using Acorn.Wasm.Encode;
using Acorn.Wasm.Decode;
using Acorn.Wasm.Scanner;
using Acorn.SpirV.Data;
using Acorn.SpirV.Encode;
using Acorn.SpirV.Decode;
using Acorn.SpirV.Scanner;
using Acorn.ELF.Data;
using Acorn.ELF.Encode;
using Acorn.ELF.Decode;
using Acorn.Nyar.Data;
using Acorn.Nyar.Encode;
using Xunit;

namespace Acorn.Tests;

public class CrossFormatConsistencyTests
{
    #region 确定性编码测试

    [Fact]
    public void Encode_Deterministic_Wasm_SameInput_Produces_SameBytes()
    {
        var module = CreateWasmModule();

        var bytes1 = WasmEncoder.Encode(module);
        var bytes2 = WasmEncoder.Encode(module);

        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void Encode_Deterministic_SpirV_SameInput_Produces_SameBytes()
    {
        var module = CreateSpirvModule();

        var encoder = new SpirvEncoder();
        var bytes1 = encoder.Encode(module);
        var bytes2 = encoder.Encode(module);

        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void Encode_Deterministic_ELF_SameInput_Produces_SameBytes()
    {
        var file = CreateElfFile();

        var encoder = new ElfEncoder();
        var bytes1 = encoder.Encode(file);
        var bytes2 = encoder.Encode(file);

        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void Encode_Deterministic_Nyar_SameInput_Produces_SameBytes()
    {
        var module = CreateNyarModule();

        var encoder = new NyarEncoder();
        var bytes1 = encoder.Encode(module);
        var bytes2 = encoder.Encode(module);

        Assert.Equal(bytes1, bytes2);
    }

    #endregion

    #region 扫描器-解码器一致性测试

    [Fact]
    public void Scanner_Decoder_Consistency_Wasm()
    {
        var module = CreateWasmModule();
        var bytes = WasmEncoder.Encode(module);

        var summary = WasmScanner.Scan(bytes);
        var decoded = WasmDecoder.Decode(bytes);

        Assert.NotNull(summary);
        Assert.NotNull(decoded);
        Assert.Equal(module.Version, decoded.Version);
    }

    [Fact]
    public void Scanner_Decoder_Consistency_SpirV()
    {
        var module = CreateSpirvModule();
        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(module);

        var scanner = new SpirvScanner(bytes);
        var summary = scanner.Scan();

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(summary);
        Assert.NotNull(decoded);
        Assert.Equal(module.Bound, decoded.Bound);
    }

    [Fact]
    public void Scanner_Decoder_Consistency_ELF()
    {
        var file = CreateElfFile();
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(file);

        var scanResult = ELFScanner.Scan(bytes);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(scanResult);
        Assert.Contains("ELF File Scan Result", scanResult);
        Assert.NotNull(decoded);
        Assert.Equal(file.Header.Type, decoded.Header.Type);
        Assert.Equal(file.Header.Machine, decoded.Header.Machine);
    }

    #endregion

    #region 字节缓冲区正确性测试

    [Fact]
    public void ByteBuffer_RoundTrip_ReadAllBytes_PreservesData()
    {
        var original = new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        var buffer = new ByteBuffer(original);

        var result = new byte[original.Length];

        for (var i = 0; i < original.Length; i++)
        {
            result[i] = buffer.ReadByte();
        }

        Assert.Equal(original, result);
    }

    [Fact]
    public void ByteBufferWriter_RoundTrip_WriteThenVerify_PreservesData()
    {
        var original = new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        var destination = new byte[original.Length];
        var writer = new ByteBufferWriter(destination);

        for (var i = 0; i < original.Length; i++)
        {
            writer.WriteByte(original[i]);
        }

        Assert.Equal(original, destination);
    }

    [Fact]
    public void ByteBufferWriter_WriteMultiFormat_Then_ReadBack_Matches()
    {
        var data = new byte[256];
        var writer = new ByteBufferWriter(data);
        writer.WriteU32LE(0x12345678);
        writer.WriteU16BE(0xABCD);
        writer.WriteI32LE(-1);
        writer.WriteI64LE(42L);

        var written = writer.WrittenData;
        var buffer = new ByteBuffer(written);

        Assert.Equal(0x12345678u, buffer.ReadU32LE());
        Assert.Equal((ushort)0xABCD, buffer.ReadU16BE());
        Assert.Equal(-1, buffer.ReadI32LE());
        Assert.Equal(42L, buffer.ReadI64LE());
    }

    #endregion

    #region 格式头部魔数一致性测试

    [Fact]
    public void Wasm_Header_Magic_And_Version_Bytes_Correct()
    {
        var module = CreateWasmModule();
        var bytes = WasmEncoder.Encode(module);

        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x61, bytes[1]);
        Assert.Equal(0x73, bytes[2]);
        Assert.Equal(0x6D, bytes[3]);
        Assert.Equal(0x01, bytes[4]);
        Assert.Equal(0x00, bytes[5]);
        Assert.Equal(0x00, bytes[6]);
        Assert.Equal(0x00, bytes[7]);
    }

    [Fact]
    public void SpirV_Header_Magic_Bytes_Correct()
    {
        var module = CreateSpirvModule();
        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(module);

        Assert.True(bytes.Length >= 20);
        Assert.Equal(0x03, bytes[0]);
        Assert.Equal(0x02, bytes[1]);
        Assert.Equal(0x23, bytes[2]);
        Assert.Equal(0x07, bytes[3]);
    }

    [Fact]
    public void ELF_Header_Magic_Bytes_Correct()
    {
        var file = CreateElfFile();
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(file);

        Assert.Equal(0x7F, bytes[0]);
        Assert.Equal((byte)'E', bytes[1]);
        Assert.Equal((byte)'L', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    #endregion

    #region 跨格式字节序独立性测试

    [Fact]
    public void Endianness_LE_BE_Writes_Different_Bytes_For_MultiByteValues()
    {
        var leBuf = new byte[4];
        var beBuf = new byte[4];

        var leWriter = new ByteBufferWriter(leBuf);
        leWriter.WriteU32LE(0x12345678);

        var beWriter = new ByteBufferWriter(beBuf);
        beWriter.WriteU32BE(0x12345678);

        Assert.NotEqual(leBuf, beBuf);

        var leReader = new ByteBuffer(leBuf);
        var beReader = new ByteBuffer(beBuf);

        Assert.Equal(0x12345678u, leReader.ReadU32LE());
        Assert.Equal(0x12345678u, beReader.ReadU32BE());
    }

    [Fact]
    public void Endianness_32Bit_RoundTrip_PreservesValue()
    {
        var buf = new byte[4];
        var testValues = new uint[] { 0, 1, 0xFFFFFFFF, 0x12345678, 0x80000000 };

        foreach (var original in testValues)
        {
            var writer = new ByteBufferWriter(buf);
            writer.WriteU32LE(original);

            var reader = new ByteBuffer(buf);

            Assert.Equal(original, reader.ReadU32LE());
        }
    }

    #endregion

    #region 辅助方法

    private static WasmModuleData CreateWasmModule()
    {
        return new WasmModuleData
        {
            Version = 1,
            Types =
            [
                new WasmFunctionType
                {
                    Parameters = [],
                    Results = [WasmValueType.I32]
                }
            ],
            FunctionTypeIndices = [0],
            Exports =
            [
                new WasmExport
                {
                    Name = "add",
                    Kind = WasmExternalKind.Function,
                    Index = 0
                }
            ],
            Codes =
            [
                new WasmCode
                {
                    Locals = [],
                    Body = [0x20, 0x00, 0x20, 0x01, 0x6A, 0x0B]
                }
            ],
            Tables = [],
            Memories = [],
            Globals = [],
            Elements = [],
            DataSegments = [],
            Imports = [],
            Tags = [],
            CustomSections = []
        };
    }

    private static SpirvModuleData CreateSpirvModule()
    {
        return new SpirvModuleData
        {
            MagicNumber = 0x07230203,
            Version = 0x00010600,
            GeneratorMagic = 0x00030001,
            Bound = 6,
            Schema = 0,
            Instructions =
            [
                new SpirvInstruction { Opcode = SpirvOpCode.OpMemoryModel, Operands = [0, 0] },
                new SpirvInstruction { Opcode = SpirvOpCode.OpEntryPoint, Operands = [4] },
                new SpirvInstruction { Opcode = SpirvOpCode.OpReturn }
            ],
            EntryPoints = [],
            Decorations = [],
            Names = [],
            Types = []
        };
    }

    private static ELFFileData CreateElfFile()
    {
        return new ELFFileData
        {
            Header = new ELFHeaderData
            {
                Magic = [0x7F, 0x45, 0x4C, 0x46],
                Class = ElfConstants.Class64,
                DataEncoding = ElfConstants.DataEncodingLittleEndian,
                Version = 1,
                OSABI = 0,
                ABIVersion = 0,
                Type = ElfConstants.TypeExecutable,
                Machine = 0x3E,
                EntryPoint = 0
            },
            SectionHeaders = [],
            ProgramHeaders = [],
            SectionNames = []
        };
    }

    private static NyarModuleData CreateNyarModule()
    {
        return new NyarModuleData
        {
            Version = 1,
            Name = "test",
            Constants = [],
            Functions =
            [
                new NyarFunction
                {
                    Name = "main",
                    Arity = 0,
                    LocalCount = 0,
                    CodeOffset = 0,
                    CodeLength = 0
                }
            ],
            Imports = [],
            Exports = []
        };
    }

    #endregion
}
