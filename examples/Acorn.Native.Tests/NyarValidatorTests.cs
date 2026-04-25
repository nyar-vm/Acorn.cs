using Acorn.Nyar.Data;
using Acorn.Nyar.Validate;

namespace Acorn.Native.Tests;

public sealed class NyarValidatorTests
{
    #region 有效模块验证

    [Fact]
    public void Validate_ValidModule_ReturnsTrue()
    {
        var module = CreateValidModule();
        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_ValidModuleWithImports_ReturnsTrue()
    {
        var module = CreateValidModuleWithImports();
        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    #endregion

    #region 无效代码偏移

    [Fact]
    public void Validate_CodeOffsetExceedsBytecodeLength_ReturnsFalse()
    {
        var module = CreateModuleWithFunction(codeOffset: 1000, codeLength: 10);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("invalid code offset"));
    }

    [Fact]
    public void Validate_CodeRangeExceedsBytecodeLength_ReturnsFalse()
    {
        var module = CreateModuleWithFunction(codeOffset: 50, codeLength: 100);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("exceeds bytecode length"));
    }

    [Fact]
    public void Validate_NegativeCodeOffset_ReturnsFalse()
    {
        var module = CreateModuleWithFunction(codeOffset: -1, codeLength: 10);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("invalid code offset"));
    }

    #endregion

    #region 无效参数/局部变量数量

    [Fact]
    public void Validate_NegativeArity_ReturnsFalse()
    {
        var module = CreateModuleWithFunction(arity: -1);
        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("negative arity"));
    }

    [Fact]
    public void Validate_NegativeLocalCount_ReturnsFalse()
    {
        var module = CreateModuleWithFunction(localCount: -1);
        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("negative local count"));
    }

    #endregion

    #region 无效导入

    [Fact]
    public void Validate_EmptyModuleName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [new NyarImport { ModuleName = "", SymbolName = "func" }],
            Exports = []
        };

        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("module or symbol name is empty"));
    }

    [Fact]
    public void Validate_EmptySymbolName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction
                {
                    Name = "main", Arity = 0, LocalCount = 0,
                    CodeOffset = 0, CodeLength = 1
                }
            ],
            Constants = [],
            Imports = [new NyarImport { ModuleName = "mod", SymbolName = "" }],
            Exports = []
        };

        var bytecode = CreateValidBytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("module or symbol name is empty"));
    }

    #endregion

    #region 未定义操作码

    [Fact]
    public void Validate_UndefinedOpcode_ReturnsFalse()
    {
        var bytecode = new byte[] { 0xFF };
        var module = CreateModuleWithFunction(codeOffset: 0, codeLength: 1);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("undefined opcode"));
    }

    #endregion

    #region 跳转目标验证

    [Fact]
    public void Validate_JumpTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 100);

        var module = CreateModuleWithFunction(codeOffset: 0, codeLength: 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("jump target") && d.Contains("out of range"));
    }

    [Fact]
    public void Validate_JumpTargetInSameFunction_ReturnsTrue()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 5);
        bytecode[5] = (byte)NyarOpcode.Nop;

        var module = CreateModuleWithFunction(codeOffset: 0, codeLength: 6);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_JumpIfTrueTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.JumpIfTrue;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), -1);

        var module = CreateModuleWithFunction(codeOffset: 0, codeLength: 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_JumpIfFalseTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.JumpIfFalse;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 9999);

        var module = CreateModuleWithFunction(codeOffset: 0, codeLength: 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    #endregion

    #region NyarOpcode 枚举验证

    [Theory]
    [InlineData(NyarOpcode.Nop, 0x00)]
    [InlineData(NyarOpcode.Jump, 0x01)]
    [InlineData(NyarOpcode.Return, 0x05)]
    [InlineData(NyarOpcode.Const, 0x10)]
    [InlineData(NyarOpcode.LoadLocal, 0x20)]
    [InlineData(NyarOpcode.StoreLocal, 0x21)]
    [InlineData(NyarOpcode.I32Add, 0x30)]
    [InlineData(NyarOpcode.I32Eq, 0x40)]
    [InlineData(NyarOpcode.I64Add, 0x50)]
    [InlineData(NyarOpcode.F32Add, 0x60)]
    [InlineData(NyarOpcode.F64Add, 0x70)]
    [InlineData(NyarOpcode.I32ExtendI64S, 0x80)]
    [InlineData(NyarOpcode.Alloc, 0x90)]
    [InlineData(NyarOpcode.NewObject, 0xA0)]
    [InlineData(NyarOpcode.StringConcat, 0xB0)]
    [InlineData(NyarOpcode.BigIntAdd, 0xC0)]
    [InlineData(NyarOpcode.Print, 0xD0)]
    [InlineData(NyarOpcode.BuiltinCall, 0xE0)]
    public void NyarOpcode_HasCorrectValue(NyarOpcode opcode, byte expectedValue)
    {
        Assert.Equal(expectedValue, (byte)opcode);
    }

    #endregion

    #region 辅助方法

    private static NyarModuleData CreateValidModule()
    {
        return new NyarModuleData
        {
            Name = "test_module",
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
    }

    private static NyarModuleData CreateValidModuleWithImports()
    {
        return new NyarModuleData
        {
            Name = "test_module",
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
                new NyarImport { ModuleName = "math", SymbolName = "sin", Kind = NyarImportKind.Function }
            ],
            Exports = []
        };
    }

    private static byte[] CreateValidBytecode()
    {
        return [(byte)NyarOpcode.Return];
    }

    private static NyarModuleData CreateModuleWithFunction(
        int codeOffset = 0, int codeLength = 1, int arity = 0, int localCount = 0)
    {
        return new NyarModuleData
        {
            Name = "test",
            Functions =
            [
                new NyarFunction
                {
                    Name = "func", Arity = arity, LocalCount = localCount,
                    CodeOffset = codeOffset, CodeLength = codeLength
                }
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };
    }

    #endregion
}
