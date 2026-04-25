using Acorn.Nyar.Data;
using Acorn.Nyar.Validate;

namespace Acorn.Native.Tests;

public sealed class NyarValidatorAdvancedTests
{
    #region 多函数模块验证

    [Fact]
    public void Validate_MultipleFunctions_AllValid_ReturnsTrue()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarOpcode.Return;
        bytecode[5] = (byte)NyarOpcode.Nop;
        bytecode[6] = (byte)NyarOpcode.Return;

        var module = new NyarModuleData
        {
            Name = "multi_func",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func1", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                },
                new NyarFunction
                {
                    Name = "func2", Arity = 1, LocalCount = 2,
                    CodeOffset = 5, CodeLength = 2
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_MultipleFunctions_OneInvalid_ReturnsFalse()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarOpcode.Return;
        bytecode[5] = (byte)NyarOpcode.Return;

        var module = new NyarModuleData
        {
            Name = "multi_func",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func1", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                },
                new NyarFunction
                {
                    Name = "func2", Arity = -1, LocalCount = 0,
                    CodeOffset = 5, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.True(diagnostics.Count >= 1);
    }

    #endregion

    #region 函数重叠检测

    [Fact]
    public void Validate_OverlappingFunctions_ReturnsFalse()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarOpcode.Return;
        bytecode[5] = (byte)NyarOpcode.Return;

        var module = new NyarModuleData
        {
            Name = "overlap",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func1", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 10
                },
                new NyarFunction
                {
                    Name = "func2", Arity = 0, LocalCount = 0,
                    CodeOffset = 5, CodeLength = 5
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    #endregion

    #region 常量验证

    [Fact]
    public void Validate_ValidConstants_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "constants",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants =
            [
                new NyarConstant { Kind = NyarConstantKind.Int32, Value = 42 },
                new NyarConstant { Kind = NyarConstantKind.Float64, Value = 3.14 },
                new NyarConstant { Kind = NyarConstantKind.Bool, Value = true },
                new NyarConstant { Kind = NyarConstantKind.Null, Value = null },
                new NyarConstant { Kind = NyarConstantKind.String, Value = "hello" }
            ],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.True(result);
    }

    #endregion

    #region 导出验证

    [Fact]
    public void Validate_ValidExports_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "exports",
            Functions =
            [
                new NyarFunction
                {
                    Name = "add", Arity = 2, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport { Kind = NyarExportKind.Function, SymbolName = "add", FunctionIndex = 0 }
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.True(result);
    }

    [Fact]
    public void Validate_ExportFunctionIndexOutOfRange_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "bad_exports",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport { Kind = NyarExportKind.Function, SymbolName = "missing", FunctionIndex = 99 }
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_ExportEmptySymbolName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "bad_exports",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport { Kind = NyarExportKind.Function, SymbolName = "", FunctionIndex = 0 }
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.False(result);
    }

    #endregion

    #region 空模块验证

    [Fact]
    public void Validate_EmptyModule_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "empty",
            Functions = [],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [], out var diagnostics);

        Assert.True(result);
    }

    [Fact]
    public void Validate_EmptyModuleName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_EmptyFunctionName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test",
            Functions =
            [
                new NyarFunction
                {
                    Name = "", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out var diagnostics);

        Assert.False(result);
    }

    #endregion

    #region 零长度代码验证

    [Fact]
    public void Validate_ZeroCodeLength_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "zero_code",
            Functions =
            [
                new NyarFunction
                {
                    Name = "empty_func", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 0
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [], out var diagnostics);

        Assert.False(result);
    }

    #endregion

    #region 跳转指令边界

    [Fact]
    public void Validate_JumpToExactBoundary_ReturnsTrue()
    {
        var bytecode = new byte[5];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 5);

        var module = new NyarModuleData
        {
            Name = "jump_boundary",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 5
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_JumpToOnePastBoundary_ReturnsFalse()
    {
        var bytecode = new byte[5];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 6);

        var module = new NyarModuleData
        {
            Name = "jump_boundary",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 5
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out _);

        Assert.False(result);
    }

    #endregion

    #region NyarOpcode 枚举完整性

    [Fact]
    public void NyarOpcode_AllValuesAreUnique()
    {
        var values = Enum.GetValues<NyarOpcode>();
        var uniqueValues = new HashSet<int>();

        foreach (var value in values)
        {
            Assert.True(uniqueValues.Add((int)value), $"重复的 NyarOpcode 值: {value}");
        }
    }

    [Fact]
    public void NyarOpcode_NopIs0()
    {
        Assert.Equal(0, (int)NyarOpcode.Nop);
    }

    [Fact]
    public void NyarOpcode_ReturnIs5()
    {
        Assert.Equal(5, (int)NyarOpcode.Return);
    }

    #endregion

    #region 导入类型验证

    [Fact]
    public void Validate_FunctionImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports =
            [
                new NyarImport { Kind = NyarImportKind.Function, ModuleName = "math", SymbolName = "sin" }
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_GlobalImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports =
            [
                new NyarImport { Kind = NyarImportKind.Global, ModuleName = "env", SymbolName = "PI" }
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_ModuleImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports =
            [
                new NyarImport { Kind = NyarImportKind.Module, ModuleName = "utils", SymbolName = "*" }
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarOpcode.Return], out _);

        Assert.True(result);
    }

    #endregion
}
