# 🌰 Nyar.Vendor.Acorn

[![NuGet](https://img.shields.io/nuget/v/Nyar.Vendor.Acorn.svg)](https://www.nuget.org/packages/Nyar.Vendor.Acorn)
[![License](https://img.shields.io/github/license/nyar-vm/Acorn.cs.svg)](https://github.com/nyar-vm/Acorn.cs)

**Complete Acorn Binary Format Processing Package** - A unified entry point for all Acorn codecs, providing one-stop binary format encoding and decoding solutions.

## ✨ Features

- 🚀 **High Performance** - Zero-allocation design, SIMD optimized
- 📦 **Full Format Coverage** - Supports 20+ mainstream binary formats
- 🔧 **Unified API** - Consistent codec interface design
- 🛡️ **Type Safe** - Complete .NET type system support
- 📝 **Source Generator** - Auto-generated serialization code

## 📦 Included Codecs

### 🔧 Core Foundation

| Package | Format | Description |
|---|---|---|
| Acorn | Core | Binary primitive types, LEB128, variable-length integers, frame protocol |
| Generator.Acorn | - | Binary serialization source generator |

### 🎨 Graphics & Media

| Package | Format | Description |
|---|---|---|
| Acorn.Gltf | `.glb` | glTF 2.0 binary format |
| Acorn.Spine | `.skel` | Spine skeletal animation binary format |
| Acorn.Live2D | `.moc3` | Live2D Cubism model format |
| Acorn.Psd | `.psd` | Photoshop document format |
| Acorn.SpirV | `.spv` | SPIR-V shader intermediate representation |

### ⚙️ Executable Formats

| Package | Format | Description |
|---|---|---|
| Acorn.Elf | `.elf` | Linux Executable and Linkable Format |
| Acorn.Pe | `.exe`/`.dll` | Windows Portable Executable |
| Acorn.MachO | `.dylib`/`.app` | macOS executable file format |
| Acorn.Coff | `.obj`/`.lib` | Common Object File Format |
| Acorn.Dwarf | `.debug` | DWARF debug information format |

### 🔨 Compiler Intermediate Representations

| Package | Format | Description |
|---|---|---|
| Acorn.Llvm | `.bc` | LLVM Bitcode intermediate representation |
| Acorn.Wasm | `.wasm` | WebAssembly binary format |

### 🤖 Machine Learning

| Package | Format | Description |
|---|---|---|
| Acorn.Onnx | `.onnx` | Open Neural Network Exchange |
| Acorn.SafeTensors | `.safetensors` | Safe tensor format |

### 🗄️ Database Protocols

| Package | Format | Description |
|---|---|---|
| Acorn.MySQL | MySQL Protocol | MySQL database communication protocol |
| Acorn.PostgreSQL | PostgreSQL Protocol | PostgreSQL database communication protocol |

### 📨 Messaging Protocols

| Package | Format | Description |
|---|---|---|
| Acorn.Protobuf | `.proto` | Protocol Buffers binary format |
| Acorn.Redis | RESP | Redis Serialization Protocol |
| Acorn.ZeroMQ | ZMTP | ZeroMQ Message Transport Protocol |

### 📄 Documents & Compression

| Package | Format | Description |
|---|---|---|
| Acorn.Office | `.docx`/`.xlsx`/`.pptx` | Office Open XML format |
| Acorn.Zip | `.zip` | ZIP archive format |

## 📥 Installation

### Install via NuGet

```bash
dotnet add package Nyar.Vendor.Acorn
```

### Install via Package Manager

```powershell
Install-Package Nyar.Vendor.Acorn
```

## 🚀 Quick Start

### Decode Binary Files

```csharp
using Acorn;
using Acorn.Gltf;

// Decode a glTF file
var decoder = new GltfDecoder();
var model = decoder.Decode(File.ReadAllBytes("model.glb"));

Console.WriteLine($"Mesh count: {model.Meshes.Count}");
Console.WriteLine($"Material count: {model.Materials.Count}");
```

### Encode Binary Files

```csharp
using Acorn;
using Acorn.Gltf;

// Encode a glTF file
var encoder = new GltfEncoder();
var data = encoder.Encode(model);
File.WriteAllBytes("output.glb", data);
```

### Use Frame Scanner

```csharp
using Acorn.Frame;

// Create a frame scanner
var scanner = new FrameScanner(buffer);
while (scanner.HasNext)
{
    var frame = scanner.ScanNext();
    // Process frame data
}
```

### Use Variable-Length Integer Codec

```csharp
using Acorn.Codec;

// LEB128 encoding
var encoded = VarIntCodecs.EncodeLEB128(12345);

// LEB128 decoding
var (value, bytesRead) = VarIntCodecs.DecodeLEB128(encoded);
```

### Use Binary Serialization Source Generator

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

// Generated code automatically provides Encode/Decode methods
var data = new MyData { Id = 1, Name = "Test" };
var bytes = data.Encode();
var decoded = MyData.Decode(bytes);
```
