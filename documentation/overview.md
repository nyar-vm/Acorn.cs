# 🌰 Acorn Binary Framework

[![NuGet](https://img.shields.io/nuget/v/Acorn.svg)](https://www.nuget.org/packages/Acorn)
[![License](https://img.shields.io/github/license/nyar-vm/Acorn.cs.svg)](https://github.com/nyar-vm/Acorn.cs)

**高性能、声明式优先的二进制解析/写入框架**

## ✨ 核心特性

- 🚀 **高性能** - 零分配设计，SIMD 优化，完全内联
- 📝 **声明式优先** - 特性标记 + 源生成器，手写代码零行
- 🔧 **类型安全** - 完整的 .NET 类型系统支持
- 🛡️ **零反射** - 编译时生成，运行时零虚调用
- 📦 **模块化** - 核心库稳定，协议包按需引入

## 🎯 设计哲学

> **声明式描述结构，过程式定义策略。**  
> 特性（Attribute）负责表达数据 **形状**，接口（Interface）负责实现解析 **算法**。

Acorn Binary Framework 并不试图用一套声明式标记覆盖所有可能的二进制格式。它的设计边界非常清晰：

- **约 90% 的常见场景**（固定偏移、长度前缀、条件字段、位域、偏移表、代数帧）通过纯特性声明即可完成解析与写入，手写代码零行。
- **剩余 10% 的复杂协议**（自描述格式、指令流判别、校验/纠错、跨帧状态）通过实现 **轻量级接口**（通常 30–100 行代码）来完成，而数据字段的定义**依然通过特性声明**。

框架在编译时通过 **源生成器** 将特性标记的 `partial struct` 转化为完全内联、零虚调用、零反射的读取/写入代码，性能等同于纯手写。

## 📚 文档目录

| 文档 | 说明 |
|---|---|
| [架构设计](./architecture.md) | 核心接口与基础设施 |
| [特性系统](./attributes.md) | 声明式特性标记 |
| [源生成器](./source-generator.md) | 生成器契约与行为 |
| [典型用例](./examples.md) | GLB、Live2D、WASM 等示例 |
| [扩展点](./extensibility.md) | 自定义编解码器与协议 |
| [常见问题](./faq.md) | FAQ 与最佳实践 |

## 🚀 快速开始

### 安装

```bash
dotnet add package Acorn
```

### 基本用法

```csharp
using Acorn;
using Acorn.Attributes;

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct GlbHeader
{
    [Field(Order = 0, Length = 4)] public FixedBytes4 Magic;
    [Field(Order = 1)] public uint Version;
    [Field(Order = 2)] public uint Length;
    [Field(Order = 3, LengthField = nameof(Length))] public byte[] ChunkData;
}

// 解析
var buffer = new ByteBuffer(rawData);
if (GlbHeader.TryRead(ref buffer, out var header))
{
    Console.WriteLine($"Version: {header.Version}");
}

// 写入
var writer = new ByteBufferWriter(buffer);
header.WriteTo(ref writer);
```

## 🏗️ 项目结构

```
Acorn.cs/
├── projects/
│   ├── Acorn/              # 核心库
│   ├── Generator.Acorn/    # 源生成器
│   ├── Acorn.Gltf/         # glTF 格式
│   ├── Acorn.Wasm/         # WebAssembly 格式
│   ├── Acorn.Protobuf/     # Protocol Buffers
│   └── ...                 # 其他格式支持
├── documentation/          # 文档
└── vendors/               # NuGet 打包配置
```

## 📄 许可证

本项目采用 [MPL-2.0](https://github.com/nyar-vm/Acorn.cs/blob/main/LICENSE) 许可证。
