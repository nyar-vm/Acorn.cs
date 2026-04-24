# 📦 Acorn.SpirV

SPIR-V（Standard Portable Intermediate Representation）着色器中间表示编解码器。

## 📐 格式布局

### SPIR-V 文件头（20 字节）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| MagicNumber | 0x00 | 4 | `0x07230203`（小端序） | `SpirvFileHeader.MagicNumber` |
| Version | 0x04 | 4 | 版本号（如 0x00010300 = 1.3） | `SpirvFileHeader.Version` |
| GeneratorMagic | 0x08 | 4 | 生成器工具标识 | `SpirvFileHeader.GeneratorMagic` |
| Bound | 0x0C | 4 | ID 绑定值（所有 ID < Bound） | `SpirvFileHeader.Bound` |
| Schema | 0x10 | 4 | 保留字（通常为0） | `SpirvFileHeader.Schema` |

### SPIR-V 指令格式

每个指令由 32 位字组成：

| 字 | 说明 |
|---|---|
| Word 0 | 低 16 位：指令长度（字）；高 16 位：操作码 |
| Word 1..N | 操作数 |

### 常见操作码类别

| 类别 | 说明 |
|---|---|
| OpNop ~ OpSourceContinued | 调试和注释 |
| OpName ~ OpMemberDecorate | 注解和装饰 |
| OpExtension ~ OpMemoryModel | 模式和设置 |
| OpEntryPoint ~ OpExecutionMode | 执行声明 |
| OpTypeVoid ~ OpTypeForwardPointer | 类型声明 |
| OpConstantTrue ~ OpSpecConstantOp | 常量 |
| OpFunction ~ OpFunctionEnd | 函数 |
| OpLabel ~ OpUnreachable | 控制流 |
| OpVariable | 变量 |
| OpLoad ~ OpStore | 内存操作 |
| OpAccessChain | 访问链 |
| OpVectorShuffle | 向量操作 |
| OpImageSampleImplicitLod ~ OpImageWrite | 图像操作 |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `SpirvFileHeader` | SPIR-V 文件头 | [Data/SpirvModuleData.cs](Data/SpirvModuleData.cs) |
| `SpirvModuleData` | SPIR-V 模块完整数据 | [Data/SpirvModuleData.cs](Data/SpirvModuleData.cs) |
| `SpirvConstants` | SPIR-V 常量 | [Data/SpirvConstants.cs](Data/SpirvConstants.cs) |
| `SpirvInstruction` | SPIR-V 指令 | [Data/SpirvModuleData.cs](Data/SpirvModuleData.cs) |
| `SpirvDecoder` | SPIR-V 解码器 | [Decode/SpirvDecoder.cs](Decode/SpirvDecoder.cs) |
| `SpirvEncoder` | SPIR-V 编码器 | [Encode/SpirvEncoder.cs](Encode/SpirvEncoder.cs) |
| `SpirvScanner` | SPIR-V 扫描器 | [Scanner/SpirvScanner.cs](Scanner/SpirvScanner.cs) |

## 📚 格式规范参考

- [SPIR-V Specification](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html)
- [SPIR-V Instruction Set](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.pdf)
- [Khronos SPIR-V GitHub](https://github.com/KhronosGroup/SPIRV-Tools)
