using Acorn.Frame;
using Acorn.Wasm.Data;
using Acorn.Wasm.Encode;
using Xunit;

namespace Acorn.Tests;

public class CrossFormatConsistencyTests
{
    #region 确定性编码测试

    [Fact]
    public void Encode_Deterministic_Wasm_SameInput_Produces_SameBytes()
    {
        var module = CreateWasmModule();

        var buf1 = new byte[256];
        var buf2 = new byte[256];
        WasmEncoder.EncodeModule(buf1, module);
        WasmEncoder.EncodeModule(buf2, module);

        var len1 = GetWrittenLength(buf1);
        var len2 = GetWrittenLength(buf2);

        Assert.Equal(len1, len2);
        Assert.Equal(buf1.AsSpan(0, len1).ToArray(), buf2.AsSpan(0, len2).ToArray());
    }

    #endregion

    #region 字节缓冲区正确性测试

    [Fact]
    public void ByteBuffer_ReadBytes_PreservesData()
    {
        var original = new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        var buffer = new ByteBuffer(original);
        var result = buffer.ReadBytes(original.Length);

        Assert.Equal(original, result.ToArray());
    }

    [Fact]
    public void ByteBufferWriter_WriteU8_PreservesData()
    {
        var writer = new ByteBufferWriter(8);
        var original = new byte[] { 0x00, 0x61, 0x73, 0x6D };

        for (var i = 0; i < original.Length; i++)
        {
            writer.WriteU8(original[i]);
        }

        Assert.Equal(original, writer.WrittenData.ToArray());
    }

    [Fact]
    public void ByteBufferWriter_WriteMultiFormat_ReadBack_Matches()
    {
        var writer = new ByteBufferWriter(256);
        writer.WriteU32LE(0x12345678);
        writer.WriteU16BE(0xABCD);
        writer.WriteI32LE(-1);
        writer.WriteI64LE(42L);

        var buffer = new ByteBuffer(writer.WrittenData);
        Assert.Equal(0x12345678u, buffer.ReadU32LE());
        Assert.Equal((ushort)0xABCD, buffer.ReadU16BE());
        Assert.Equal(-1, buffer.ReadI32LE());
        Assert.Equal(42L, buffer.ReadI64LE());
    }

    #endregion

    #region Wasm 头部魔数测试

    [Fact]
    public void Wasm_Header_Magic_Bytes_Correct()
    {
        var module = CreateWasmModule();
        var buf = new byte[256];
        var len = WasmEncoder.EncodeModule(buf, module);

        Assert.True(len >= 8);
        Assert.Equal(0x00, buf[0]);
        Assert.Equal(0x61, buf[1]);
        Assert.Equal(0x73, buf[2]);
        Assert.Equal(0x6D, buf[3]);
        Assert.Equal(0x01, buf[4]);
        Assert.Equal(0x00, buf[5]);
        Assert.Equal(0x00, buf[6]);
        Assert.Equal(0x00, buf[7]);
    }

    #endregion

    #region 字节序独立性测试

    [Fact]
    public void Endianness_LE_BE_DifferentBytes_SameValue()
    {
        var leBuf = new byte[4];
        var beBuf = new byte[4];
        var leWriter = new ByteBufferWriter(leBuf);
        var beWriter = new ByteBufferWriter(beBuf);

        leWriter.WriteU32LE(0x12345678);
        beWriter.WriteU32BE(0x12345678);

        var leBytes = leWriter.WrittenData.ToArray();
        var beBytes = beWriter.WrittenData.ToArray();

        Assert.NotEqual(leBytes, beBytes);

        var leReader = new ByteBuffer(leBytes);

        Assert.Equal(0x12345678u, leReader.ReadU32LE());

        var beReader = new ByteBuffer(beBytes);

        Assert.Equal(0x12345678u, beReader.ReadU32BE());
    }

    [Fact]
    public void Endianness_32Bit_RoundTrip_EdgeCases()
    {
        var testValues = new uint[] { 0, 1, 0xFFFFFFFF, 0x12345678, 0x80000000, 0x7FFFFFFF };

        foreach (var original in testValues)
        {
            var buf = new byte[4];
            var writer = new ByteBufferWriter(buf);
            writer.WriteU32LE(original);
            var x = writer.WrittenData;
            var reader = new ByteBuffer(x);

            Assert.Equal(original, reader.ReadU32LE());
        }
    }

    [Fact]
    public void Endianness_64Bit_RoundTrip_EdgeCases()
    {
        var testValues = new ulong[] { 0, 1, 0xFFFFFFFFFFFFFFFF, 0x123456789ABCDEF0, 0x8000000000000000 };

        foreach (var original in testValues)
        {
            var buf = new byte[8];
            var writer = new ByteBufferWriter(buf);
            writer.WriteU64LE(original);
            var x = writer.WrittenData;
            var reader = new ByteBuffer(x);

            Assert.Equal(original, reader.ReadU64LE());
        }
    }

    #endregion

    #region LEB128 编解码一致性测试

    [Fact]
    public void Leb128_U32_RoundTrip_EdgeCases()
    {
        var testValues = new uint[] { 0, 1, 127, 128, 16383, 16384, 2097151, uint.MaxValue };

        foreach (var original in testValues)
        {
            var buf = new byte[10];
            var writer = new ByteBufferWriter(buf);
            writer.WriteLeb128U32(original);
            var reader = new ByteBuffer(writer.WrittenData);

            Assert.Equal(original, reader.ReadLeb128U32());
        }
    }

    [Fact]
    public void Leb128_I32_RoundTrip_EdgeCases()
    {
        var testValues = new int[] { 0, 1, -1, 127, -128, 128, -64, 64 };

        foreach (var original in testValues)
        {
            var buf = new byte[10];
            var writer = new ByteBufferWriter(buf);
            writer.WriteLeb128I32(original);
            var reader = new ByteBuffer(writer.WrittenData);

            Assert.Equal(original, reader.ReadLeb128I32());
        }
    }

    [Fact]
    public void Leb128_I64_RoundTrip_EdgeCases()
    {
        var testValues = new long[] { 0, 1, -1, long.MaxValue, long.MinValue, 1L << 63 };

        foreach (var original in testValues)
        {
            var buf = new byte[10];
            var writer = new ByteBufferWriter(buf);
            writer.WriteLeb128I64(original);
            var reader = new ByteBuffer(writer.WrittenData);

            Assert.Equal(original, reader.ReadLeb128I64());
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
                    Results = [WasmValueType.Int32]
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

    private static int GetWrittenLength(byte[] buf)
    {
        for (var i = buf.Length - 1; i >= 0; i--)
        {
            if (buf[i] != 0)
            {
                return i + 1;
            }
        }

        return 0;
    }

    #endregion
}
