# Acorn.Gltf

Khronos glTF / GLB 模型格式的扫描、编码和解码库。

## 格式规范参考

- [glTF 2.0 Specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html)
- [glTF Binary (GLB) Format](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#binary-glb)
- [glTF JSON Schema](https://raw.githubusercontent.com/KhronosGroup/glTF/main/specification/2.0/schema/glTF.schema.json)

## 二进制格式结构

### GLB 文件布局

```
┌──────────────────────────┐
│ Header (12 bytes)        │
│   magic: 0x46546C67     │  "glTF"
│   version: uint32        │
│   length: uint32         │
├──────────────────────────┤
│ Chunk 0: JSON            │
│   chunkLength: uint32    │
│   chunkType: 0x4E4F534A │  "JSON"
│   data: byte[]           │
├──────────────────────────┤
│ Chunk 1: Binary          │
│   chunkLength: uint32    │
│   chunkType: 0x004E4942 │  "BIN\0"
│   data: byte[]           │
└──────────────────────────┘
```

## API

```csharp
using Acorn.Gltf.Decode;
using Acorn.Gltf.Encode;
using Acorn.Gltf.Scanner;

// 解码
var decoder = new GltfDecoder();
var model = decoder.Decode(File.ReadAllBytes("model.glb"));

// 编码
var encoder = new GltfEncoder();
var bytes = encoder.Encode(model);

// 扫描
var scanner = new GltfScanner(File.ReadAllBytes("model.glb"));
var stats = scanner.ScanStatistics();
```
