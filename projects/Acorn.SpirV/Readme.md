# Acorn.Spirv

Khronos SPIR-V 着色器二进制中间语言的扫描、编码和解码库。

## 格式规范参考

- [SPIR-V Specification](https://www.khronos.org/registry/SPIR-V/specs/unified1/SPIRV.html)
- [SPIR-V Registry](https://www.khronos.org/registry/spir-v/)
- [SPIR-V Opcode Reference](https://www.khronos.org/registry/SPIR-V/specs/unified1/SPIRV.html#_a_id_instructions_a_instructions)
- [GLSL.std.450 Extended Instruction Set](https://www.khronos.org/registry/spir-v/specs/unified1/GLSL.std.450.html)
- [SPIR-V Header File](https://github.com/KhronosGroup/SPIRV-Headers/blob/main/include/spirv/unified1/spirv.h)

## 二进制格式结构

SPIR-V 整个流由 32 位字组成，使用小端序：

```
┌──────────────────────────────────────┐
│ Header (5 words = 20 bytes)          │
│   MagicNumber: 0x07230203            │
│   Version: uint32 (e.g. 0x00010300)  │
│   GeneratorMagic: uint32             │
│   Bound: uint32 (max ID + 1)         │
│   Schema: uint32 (reserved, 0)       │
├──────────────────────────────────────┤
│ Instruction Stream                   │
│   ┌────────────────────────────┐     │
│   │ Word 0: (WordCount<<16)   │     │
│   │          | Opcode          │     │
│   │ Word 1..N: Operands       │     │
│   └────────────────────────────┘     │
│   ...                                │
└──────────────────────────────────────┘
```

### 指令编码

每条指令的第一个字：
- **低 16 位**：操作码 (Opcode)
- **高 16 位**：字数 (WordCount，包含操作码字本身)

### 字符串编码

字符串以 null 终止，填充到 4 字节对齐的 32 位字序列。

### 常量定义

Acorn.Spirv.Data 中提供了完整的常量定义：

| 类 | 说明 |
|---|---|
| `SpirvConstants.MagicNumber` | 魔数 0x07230203 |
| `SpirvConstants.Capability` | 能力声明（Shader、RayTracingKHR 等） |
| `SpirvConstants.ExecutionModel` | 执行模型（Vertex、Fragment 等） |
| `SpirvConstants.StorageClass` | 存储类（Uniform、Input、Output 等） |
| `SpirvConstants.Decoration` | 装饰（Block、Binding、Location 等） |
| `SpirvConstants.BuiltIn` | 内建变量（Position、FragCoord 等） |
| `SpirvConstants.GLSLstd450` | GLSL.std.450 扩展指令 |
| `SpirvOpCode` | 操作码常量（OpTypeVoid、OpEntryPoint 等） |

## API

```csharp
using Acorn.Spirv.Decode;
using Acorn.Spirv.Encode;
using Acorn.Spirv.Scanner;

// 解码
var decoder = new SpirvDecoder();
var module = decoder.Decode(File.ReadAllBytes("shader.spv"));
var entryPoints = decoder.DecodeEntryPoints(File.ReadAllBytes("shader.spv"));

// 编码
var encoder = new SpirvEncoder();
var bytes = encoder.Encode(module);

// 扫描
var scanner = new SpirvScanner(File.ReadAllBytes("shader.spv"));
var header = scanner.ScanHeader();
var stats = scanner.ScanStatistics();
```
