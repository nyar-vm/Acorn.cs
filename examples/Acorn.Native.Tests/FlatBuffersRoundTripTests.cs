using Acorn.FlatBuffers.Data;
using Acorn.FlatBuffers.Decode;
using Acorn.FlatBuffers.Encode;
using Acorn.FlatBuffers.Scanner;

namespace Acorn.Native.Tests;

public sealed class FlatBuffersRoundTripTests
{
    #region 基本往返测试

    [Fact]
    public void EncodeDecode_SingleBoolField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Bool,
                        Value = true
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
        Assert.Equal(0, table.Fields[0].Index);
        Assert.Equal(4, table.Fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleByteField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Byte,
                        Value = (sbyte)42
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
        Assert.Equal(4, table.Fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleIntField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 12345
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
        Assert.Equal(0, table.Fields[0].Index);
        Assert.Equal(4, table.Fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleFloatField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Float,
                        Value = 3.14f
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_SingleDoubleField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Double,
                        Value = 2.718281828
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_MultipleFields_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 100
                    },
                    new()
                    {
                        Index = 1,
                        VTableOffset = 8,
                        Type = FlatBufferFieldType.Float,
                        Value = 1.5f
                    },
                    new()
                    {
                        Index = 2,
                        VTableOffset = 12,
                        Type = FlatBufferFieldType.Bool,
                        Value = true
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Equal(3, table.Fields.Count);
    }

    #endregion

    #region 文件标识符测试

    [Fact]
    public void EncodeDecode_WithFileIdentifier_Roundtrip()
    {
        var data = new FlatBufferData
        {
            FileIdentifier = "TEST",
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 42
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var fileId = decoder.ReadFileIdentifier();

        Assert.Equal("TEST", fileId);

        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    #endregion

    #region Scanner 验证

    [Fact]
    public void EncodeDecode_ScannerValidation()
    {
        var data = new FlatBufferData
        {
            FileIdentifier = "SCAN",
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 99
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var scanner = new FlatBuffersScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.Equal("SCAN", header.FileIdentifier);
        Assert.True(header.RootOffset > 0);
    }

    [Fact]
    public void EncodeDecode_WithoutFileId_ScannerValidation()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Short,
                        Value = (short)7
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var scanner = new FlatBuffersScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.True(header.RootOffset > 0);
    }

    #endregion

    #region 所有标量类型测试

    [Fact]
    public void EncodeDecode_UByte_Roundtrip()
    {
        var data = CreateSingleFieldData(FlatBufferFieldType.UByte, (byte)200);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_UShort_Roundtrip()
    {
        var data = CreateSingleFieldData(FlatBufferFieldType.UShort, (ushort)50000);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_UInt_Roundtrip()
    {
        var data = CreateSingleFieldData(FlatBufferFieldType.UInt, 0xFFFFFFFFu);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_Long_Roundtrip()
    {
        var data = CreateSingleFieldData(FlatBufferFieldType.Long, 0x7FFFFFFFFFFFFFFF);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    [Fact]
    public void EncodeDecode_ULong_Roundtrip()
    {
        var data = CreateSingleFieldData(FlatBufferFieldType.ULong, 0xFFFFFFFFFFFFFFFFu);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.Fields);
    }

    #endregion

    #region 便捷方法测试

    [Fact]
    public void EncodeTable_Directly_Roundtrip()
    {
        var table = new FlatBufferTable
        {
            Fields = new List<FlatBufferField>
            {
                new()
                {
                    Index = 0,
                    VTableOffset = 4,
                    Type = FlatBufferFieldType.Int,
                    Value = 256
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.EncodeTable(table, "DIRT");

        var decoder = new FlatBuffersDecoder(bytes);
        var fileId = decoder.ReadFileIdentifier();

        Assert.Equal("DIRT", fileId);

        var rootOffset = decoder.DecodeRootOffset();
        var decoded = decoder.DecodeTable(rootOffset);

        Assert.Single(decoded.Fields);
    }

    #endregion

    private static FlatBufferData CreateSingleFieldData(FlatBufferFieldType type, object value)
    {
        return new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = type,
                        Value = value
                    }
                }
            }
        };
    }
}
