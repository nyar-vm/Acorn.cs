# 🌰 Acorn.cs

[![NuGet](https://img.shields.io/nuget/v/Nyar.Vendor.Acorn.svg)](https://www.nuget.org/packages/Nyar.Vendor.Acorn)
[![License](https://img.shields.io/github/license/nyar-vm/Acorn.cs.svg)](https://github.com/nyar-vm/Acorn.cs)

Acorn 二进制元解析器 C# 版，一站式解决二进制格式编解码需求。

## ✨ 特性

- 🚀 **高性能** —— 零分配设计，SIMD 优化
- 📦 **全格式覆盖** —— 支持 20+ 种主流二进制格式
- 🔧 **统一 API** —— 一致的编解码器接口设计
- 🛡️ **类型安全** —— 完整的 .NET 类型系统支持
- 📝 **源生成器** —— 自动生成序列化代码

## 📦 包含的编解码器

### 🔧 核心基础

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn | Core | 二进制基础类型、LEB128、变长整数、帧协议 |
| Generator.Acorn | - | 二进制序列化源生成器 |

### 🎨 图形与媒体

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Gltf | `.glb` | glTF 2.0 二进制格式 |
| Acorn.Spine | `.skel` | Spine 骨骼动画二进制格式 |
| Acorn.Live2D | `.moc3` | Live2D Cubism 模型格式 |
| Acorn.Psd | `.psd` | Photoshop 文档格式 |
| Acorn.SpirV | `.spv` | SPIR-V 着色器中间表示 |

### ⚙️ 可执行文件格式

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Elf | `.elf` | Linux 可执行与可链接格式 |
| Acorn.Pe | `.exe`/`.dll` | Windows 可移植可执行文件 |
| Acorn.MachO | `.dylib`/`.app` | macOS 可执行文件格式 |
| Acorn.Coff | `.obj`/`.lib` | 通用对象文件格式 |
| Acorn.Dwarf | `.debug` | DWARF 调试信息格式 |

### 🔨 编译器中间表示

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Llvm | `.bc` | LLVM 位码中间表示 |
| Acorn.Wasm | `.wasm` | WebAssembly 二进制格式 |

### 🤖 机器学习

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Onnx | `.onnx` | 开放神经网络交换格式 |
| Acorn.SafeTensors | `.safetensors` | 安全张量格式 |

### 🗄️ 数据库协议

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.MySQL | MySQL 协议 | MySQL 数据库通信协议 |
| Acorn.PostgreSQL | PostgreSQL 协议 | PostgreSQL 数据库通信协议 |

### 📨 消息协议

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Protobuf | `.proto` | Protocol Buffers 二进制格式 |
| Acorn.Redis | RESP | Redis 序列化协议 |
| Acorn.ZeroMQ | ZMTP | ZeroMQ 消息传输协议 |

### 📄 文档与压缩

| 包名 | 格式 | 说明 |
|---|---|---|
| Acorn.Office | `.docx`/`.xlsx`/`.pptx` | Office Open XML 格式 |
| Acorn.Zip | `.zip` | ZIP 归档格式 |

## 📥 安装

### 通过 NuGet 安装

```bash
dotnet add package Nyar.Vendor.Acorn
```

### 通过包管理器安装

```powershell
Install-Package Nyar.Vendor.Acorn
```

## 🚀 快速开始

### 解码二进制文件

```csharp
using Acorn;
using Acorn.Gltf;

// 解码 glTF 文件
var decoder = new GltfDecoder();
var model = decoder.Decode(File.ReadAllBytes("model.glb"));

Console.WriteLine($"网格数量: {model.Meshes.Count}");
Console.WriteLine($"材质数量: {model.Materials.Count}");
```

### 编码二进制文件

```csharp
using Acorn;
using Acorn.Gltf;

// 编码 glTF 文件
var encoder = new GltfEncoder();
var data = encoder.Encode(model);
File.WriteAllBytes("output.glb", data);
```

### 使用帧扫描器

```csharp
using Acorn.Frame;

// 创建帧扫描器
var scanner = new FrameScanner(buffer);
while (scanner.HasNext)
{
    var frame = scanner.ScanNext();
    // 处理帧数据
}
```

### 使用变长整数编解码器

```csharp
using Acorn.Codec;

// LEB128 编码
var encoded = VarIntCodecs.EncodeLEB128(12345);

// LEB128 解码
var (value, bytesRead) = VarIntCodecs.DecodeLEB128(encoded);
```

### 使用二进制序列化源生成器

```csharp
using Acorn.Attributes;

[BinarySerializable]
public partial class MyData
{
    [Field(0)]
    public int Id { get; set; }

    [Field(1)]
    public string Name { get; set; }

    [Field(2)]
    public byte[] Payload { get; set; }
}

// 生成的代码自动提供 Encode/Decode 方法
var data = new MyData { Id = 1, Name = "Test" };
var bytes = data.Encode();
var decoded = MyData.Decode(bytes);
```
