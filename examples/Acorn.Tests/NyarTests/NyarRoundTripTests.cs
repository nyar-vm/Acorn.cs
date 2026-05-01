using Acorn.Nyar.Data;
using Acorn.Nyar.Decode;
using Acorn.Nyar.Encode;
using Acorn.Nyar.Scanner;

namespace Acorn.Tests.NyarTests;

public class NyarRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new NyarModuleData
        {
            Version = 1,
            Name = "test_module",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(original.Name, decoded.Name);
        Assert.Empty(decoded.Constants);
        Assert.Empty(decoded.Functions);
        Assert.Empty(decoded.Imports);
        Assert.Empty(decoded.Exports);
    }

    [Fact]
    public void Encode_Decode_FullModule()
    {
        var original = new NyarModuleData
        {
            Version = 1,
            Name = "full_module",
            Constants =
            [
                new NyarConstant { Kind = NyarConstantKind.Int32, Value = 42 },
                new NyarConstant { Kind = NyarConstantKind.Float64, Value = 3.14 },
                new NyarConstant { Kind = NyarConstantKind.Bool, Value = true },
                new NyarConstant { Kind = NyarConstantKind.Null, Value = null },
                new NyarConstant { Kind = NyarConstantKind.String, Value = "hello" },
                new NyarConstant { Kind = NyarConstantKind.BigInt, Value = new byte[] { 0, 0xFF, 0x01 } }
            ],
            Functions =
            [
                new NyarFunction { Name = "add", Arity = 2, LocalCount = 0, CodeOffset = 0, CodeLength = 8 },
                new NyarFunction { Name = "main", Arity = 0, LocalCount = 3, CodeOffset = 8, CodeLength = 16 }
            ],
            Imports =
            [
                new NyarImport { Kind = NyarImportKind.Function, ModuleName = "math", SymbolName = "sqrt" },
                new NyarImport { Kind = NyarImportKind.Global, ModuleName = "config", SymbolName = "version" }
            ],
            Exports =
            [
                new NyarExport { Kind = NyarExportKind.Function, SymbolName = "add", FunctionIndex = 0 },
                new NyarExport { Kind = NyarExportKind.Global, SymbolName = "count", FunctionIndex = -1 }
            ]
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(original.Name, decoded.Name);

        Assert.Equal(6, decoded.Constants.Count);
        Assert.Equal(NyarConstantKind.Int32, decoded.Constants[0].Kind);
        Assert.Equal(42, decoded.Constants[0].Value);
        Assert.Equal(NyarConstantKind.Float64, decoded.Constants[1].Kind);
        Assert.Equal(3.14, decoded.Constants[1].Value);
        Assert.Equal(NyarConstantKind.Bool, decoded.Constants[2].Kind);
        Assert.Equal(true, decoded.Constants[2].Value);
        Assert.Equal(NyarConstantKind.Null, decoded.Constants[3].Kind);
        Assert.Null(decoded.Constants[3].Value);
        Assert.Equal(NyarConstantKind.String, decoded.Constants[4].Kind);
        Assert.Equal("hello", decoded.Constants[4].Value);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[5].Kind);
        Assert.Equal(new byte[] { 0, 0xFF, 0x01 }, decoded.Constants[5].Value);

        Assert.Equal(2, decoded.Functions.Count);
        Assert.Equal("add", decoded.Functions[0].Name);
        Assert.Equal(2, decoded.Functions[0].Arity);
        Assert.Equal(0, decoded.Functions[0].LocalCount);
        Assert.Equal(0, decoded.Functions[0].CodeOffset);
        Assert.Equal(8, decoded.Functions[0].CodeLength);
        Assert.Equal("main", decoded.Functions[1].Name);
        Assert.Equal(0, decoded.Functions[1].Arity);
        Assert.Equal(3, decoded.Functions[1].LocalCount);
        Assert.Equal(8, decoded.Functions[1].CodeOffset);
        Assert.Equal(16, decoded.Functions[1].CodeLength);

        Assert.Equal(2, decoded.Imports.Count);
        Assert.Equal(NyarImportKind.Function, decoded.Imports[0].Kind);
        Assert.Equal("math", decoded.Imports[0].ModuleName);
        Assert.Equal("sqrt", decoded.Imports[0].SymbolName);
        Assert.Equal(NyarImportKind.Global, decoded.Imports[1].Kind);
        Assert.Equal("config", decoded.Imports[1].ModuleName);
        Assert.Equal("version", decoded.Imports[1].SymbolName);

        Assert.Equal(2, decoded.Exports.Count);
        Assert.Equal(NyarExportKind.Function, decoded.Exports[0].Kind);
        Assert.Equal("add", decoded.Exports[0].SymbolName);
        Assert.Equal(0, decoded.Exports[0].FunctionIndex);
        Assert.Equal(NyarExportKind.Global, decoded.Exports[1].Kind);
        Assert.Equal("count", decoded.Exports[1].SymbolName);
        Assert.Equal(-1, decoded.Exports[1].FunctionIndex);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[16];
        var decoder = new NyarDecoder();

        Assert.Throws<InvalidNyarDataException>(() => decoder.Decode(data));
    }

    [Fact]
    public void Decode_TooShort_Throws()
    {
        var data = new byte[4];
        var decoder = new NyarDecoder();

        Assert.ThrowsAny<Exception>(() => decoder.Decode(data));
    }

    [Fact]
    public void Encode_Decode_Int64BigIntConstants()
    {
        var maxBytes = BitConverter.GetBytes(long.MaxValue);
        var minBytes = BitConverter.GetBytes(long.MinValue);

        var original = new NyarModuleData
        {
            Name = "int64_test",
            Constants =
            [
                new NyarConstant { Kind = NyarConstantKind.BigInt, Value = maxBytes },
                new NyarConstant { Kind = NyarConstantKind.BigInt, Value = minBytes }
            ],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.Constants.Count);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[0].Kind);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[1].Kind);

        var decodedMax = (byte[])decoded.Constants[0].Value!;
        var decodedMin = (byte[])decoded.Constants[1].Value!;
        Assert.Equal(long.MaxValue, BitConverter.ToInt64(decodedMax, 0));
        Assert.Equal(long.MinValue, BitConverter.ToInt64(decodedMin, 0));
    }

    [Fact]
    public void Encode_Decode_LongModuleName()
    {
        var longName = new string('N', 256);

        var original = new NyarModuleData
        {
            Name = longName,
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Name, decoded.Name);
        Assert.Equal(256, decoded.Name.Length);
    }

    [Fact]
    public void Encode_Decode_ManyFunctions()
    {
        var functions = new List<NyarFunction>(100);
        for (var i = 0; i < 100; i++)
        {
            functions.Add(new NyarFunction
            {
                Name = $"func_{i:D3}",
                Arity = i % 4,
                LocalCount = i % 8,
                CodeOffset = i * 10,
                CodeLength = 10
            });
        }

        var original = new NyarModuleData
        {
            Name = "many_funcs",
            Constants = [],
            Functions = functions,
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(100, decoded.Functions.Count);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal($"func_{i:D3}", decoded.Functions[i].Name);
            Assert.Equal(i % 4, decoded.Functions[i].Arity);
            Assert.Equal(i % 8, decoded.Functions[i].LocalCount);
            Assert.Equal(i * 10, decoded.Functions[i].CodeOffset);
            Assert.Equal(10, decoded.Functions[i].CodeLength);
        }
    }

    [Fact]
    public void Encode_Decode_EmptyModuleVariants()
    {
        // 全部空集合
        {
            var original = new NyarModuleData
            {
                Name = "all_empty",
                Constants = [],
                Functions = [],
                Imports = [],
                Exports = []
            };

            var encoder = new NyarEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new NyarDecoder();
            var decoded = decoder.Decode(bytes);

            Assert.Equal("all_empty", decoded.Name);
            Assert.Empty(decoded.Constants);
            Assert.Empty(decoded.Functions);
            Assert.Empty(decoded.Imports);
            Assert.Empty(decoded.Exports);
        }

        // 空名称 + 空集合
        {
            var original = new NyarModuleData
            {
                Name = "",
                Constants = [],
                Functions = [],
                Imports = [],
                Exports = []
            };

            var encoder = new NyarEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new NyarDecoder();
            var decoded = decoder.Decode(bytes);

            Assert.Equal("", decoded.Name);
        }

        // 仅有一个常量，其余为空
        {
            var original = new NyarModuleData
            {
                Name = "only_const",
                Constants = [new NyarConstant { Kind = NyarConstantKind.Int32, Value = 99 }],
                Functions = [],
                Imports = [],
                Exports = []
            };

            var encoder = new NyarEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new NyarDecoder();
            var decoded = decoder.Decode(bytes);

            Assert.Single(decoded.Constants);
            Assert.Equal(NyarConstantKind.Int32, decoded.Constants[0].Kind);
            Assert.Equal(99, decoded.Constants[0].Value);
            Assert.Empty(decoded.Functions);
            Assert.Empty(decoded.Imports);
            Assert.Empty(decoded.Exports);
        }

        // 仅导入 + 导出，无函数无常量
        {
            var original = new NyarModuleData
            {
                Name = "only_import_export",
                Constants = [],
                Functions = [],
                Imports =
                [
                    new NyarImport { Kind = NyarImportKind.Function, ModuleName = "lib", SymbolName = "fn" }
                ],
                Exports =
                [
                    new NyarExport { Kind = NyarExportKind.Global, SymbolName = "VERSION", FunctionIndex = -1 }
                ]
            };

            var encoder = new NyarEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new NyarDecoder();
            var decoded = decoder.Decode(bytes);

            Assert.Empty(decoded.Constants);
            Assert.Empty(decoded.Functions);
            Assert.Single(decoded.Imports);
            Assert.Single(decoded.Exports);
            Assert.Equal("lib", decoded.Imports[0].ModuleName);
            Assert.Equal("fn", decoded.Imports[0].SymbolName);
            Assert.Equal("VERSION", decoded.Exports[0].SymbolName);
        }
    }

    [Fact]
    public void Decode_WrongMagic_ThrowsInvalidNyarDataException()
    {
        var original = new NyarModuleData
        {
            Name = "magic_test",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        // 破坏魔数字节，写入错误的标识
        bytes[0] = 0xDE;
        bytes[1] = 0xAD;
        bytes[2] = 0xBE;
        bytes[3] = 0xEF;

        var decoder = new NyarDecoder();

        var ex = Assert.Throws<InvalidNyarDataException>(() => decoder.Decode(bytes));
        Assert.Contains("无效的 .nyar 文件头", ex.Message);
    }
}

public class NyarScannerTests
{
    [Fact]
    public void IsNyar_ValidData_ReturnsTrue()
    {
        var original = new NyarModuleData
        {
            Name = "scan_test",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var scanner = new NyarScanner(bytes);
        Assert.True(scanner.IsNyar());
    }

    [Fact]
    public void IsNyar_InvalidData_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var scanner = new NyarScanner(data);
        Assert.False(scanner.IsNyar());
    }

    [Fact]
    public void ScanHeader_ReturnsCorrectInfo()
    {
        var original = new NyarModuleData
        {
            Name = "scan_header_test",
            Constants = [new NyarConstant { Kind = NyarConstantKind.Int32, Value = 1 }],
            Functions = [new NyarFunction { Name = "fn", Arity = 1, LocalCount = 0, CodeLength = 4 }],
            Imports = [new NyarImport { Kind = NyarImportKind.Function, ModuleName = "m", SymbolName = "s" }],
            Exports = [new NyarExport { Kind = NyarExportKind.Function, SymbolName = "e", FunctionIndex = 0 }]
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var scanner = new NyarScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.Equal(1u, header.Version);
        Assert.Equal("scan_header_test", header.ModuleName);
        Assert.Equal(4, header.SectionCount);
        Assert.True(header.HasConstantsSection);
        Assert.True(header.HasFunctionsSection);
        Assert.True(header.HasImportsSection);
        Assert.True(header.HasExportsSection);
    }
}
