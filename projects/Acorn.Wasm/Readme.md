# Acorn.Wasm

WebAssembly 二进制格式的扫描、编码和解码库。

## 格式规范参考

- [WebAssembly Binary Format Specification](https://webassembly.github.io/spec/core/binary/index.html)
- [WebAssembly Module Structure](https://webassembly.github.io/spec/core/binary/modules.html)
- [WASM Binary Encoding](https://webassembly.github.io/spec/core/binary/conventions.html)
- [WebAssembly Text Format (WAT)](https://webassembly.github.io/spec/core/text/index.html)

## 二进制格式结构

### WASM 模块布局

```
┌──────────────────────────────────────┐
│ Magic Number: 0x00 0x61 0x73 0x6D   │  "\0asm"
│ Version: 0x01 0x00 0x00 0x00        │  版本 1
├──────────────────────────────────────┤
│ Type Section (id=1)                  │
│   count: varuint32                   │
│   types: FuncType[]                  │
├──────────────────────────────────────┤
│ Import Section (id=2)                │
│   count: varuint32                   │
│   imports: Import[]                  │
├──────────────────────────────────────┤
│ Function Section (id=3)              │
│   count: varuint32                   │
│   type_indices: varuint32[]          │
├──────────────────────────────────────┤
│ Table Section (id=4)                 │
│ Memory Section (id=5)                │
│ Global Section (id=6)                │
│ Export Section (id=7)                │
│ Start Section (id=8)                 │
│ Element Section (id=9)               │
│ Code Section (id=10)                 │
│ Data Section (id=11)                 │
└──────────────────────────────────────┘
```

### Section 编码

每个 Section：
```
section_id: varuint7
payload_length: varuint32
payload: byte[]
```

### LEB128 编码

WASM 大量使用无符号 LEB128 (varuint32) 和有符号 LEB128 (varint32/64) 编码整数。

## API

```csharp
using Acorn.Wasm.Decode;
using Acorn.Wasm.Encode;
using Acorn.Wasm.Scanner;

// 解码
var decoder = new WasmDecoder();
var module = decoder.Decode(File.ReadAllBytes("module.wasm"));

// 编码
var encoder = new WasmEncoder();
var bytes = encoder.Encode(module);

// 扫描
var scanner = new WasmScanner(File.ReadAllBytes("module.wasm"));
var stats = scanner.ScanStatistics();
```
