# 📦 Acorn.Wasm

WebAssembly 二进制格式（`.wasm`）编解码器。

## 📐 格式布局

### Wasm 模块头

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Magic | 0x00 | 4 | `\0asm`（0x00 0x61 0x73 0x6D） | `WasmConstants.MagicNumber` |
| Version | 0x04 | 4 | 版本号（1） | `WasmConstants.Version` |

### 段（Section）

| 字段 | 大小 | 说明 |
|---|---|---|
| SectionID | 1 | 段标识符 |
| SectionSize | Varint | 段大小（字节） |
| SectionContent | N | 段内容 |

### 段类型

| ID | 名称 | 说明 | 对应类 |
|---|---|---|---|
| 0 | Custom | 自定义段 | `WasmCustomSection` |
| 1 | Type | 函数类型签名 | `WasmFunctionType` |
| 2 | Import | 导入 | `WasmImport` |
| 3 | Function | 函数类型索引 | `WasmModuleData.FunctionTypeIndices` |
| 4 | Table | 表定义 | `WasmTable` |
| 5 | Memory | 内存定义 | `WasmMemory` |
| 6 | Global | 全局变量 | `WasmGlobal` |
| 7 | Export | 导出 | `WasmExport` |
| 8 | Start | 起始函数 | `WasmModuleData.StartFunctionIndex` |
| 9 | Element | 元素段 | `WasmElement` |
| 10 | Code | 函数体 | `WasmCode` |
| 11 | Data | 数据段 | `WasmData` |
| 12 | DataCount | 数据段计数 | - |

### 值类型

| 字节 | 名称 | 说明 |
|---|---|---|
| 0x7F | i32 | 32 位整数 |
| 0x7E | i64 | 64 位整数 |
| 0x7D | f32 | 32 位浮点数 |
| 0x7C | f64 | 64 位浮点数 |
| 0x7B | v128 | 128 位向量（SIMD） |
| 0x70 | funcref | 函数引用 |
| 0x6F | externref | 外部引用 |

### 函数类型

| 字段 | 大小 | 说明 |
|---|---|---|
| Form | 1 | 0x60（函数类型标记） |
| ParamCount | Varint | 参数数量 |
| ParamTypes | N | 参数类型列表 |
| ResultCount | Varint | 返回值数量 |
| ResultTypes | N | 返回值类型列表 |

### 指令格式

| 字段 | 大小 | 说明 |
|---|---|---|
| Opcode | 1/2 | 操作码 |
| Operands | 变长 | 操作数（根据指令类型） |
| End | 1 | 0x0B（函数结束标记） |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `WasmModuleData` | Wasm 模块完整数据 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmFunctionType` | 函数类型 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmImport` | 导入项 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmExport` | 导出项 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmTable` | 表定义 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmMemory` | 内存定义 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmGlobal` | 全局变量 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmCode` | 函数体 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmData` | 数据段 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmElement` | 元素段 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmCustomSection` | 自定义段 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmConstants` | Wasm 常量 | [Data/WasmConstants.cs](Data/WasmConstants.cs) |
| `WasmSectionId` | 段 ID 枚举 | [Data/WasmConstants.cs](Data/WasmConstants.cs) |
| `WasmValueType` | 值类型枚举 | [Data/WasmModuleData.cs](Data/WasmModuleData.cs) |
| `WasmDecoder` | Wasm 解码器 | [Decode/WasmDecoder.cs](Decode/WasmDecoder.cs) |
| `WasmEncoder` | Wasm 编码器 | [Encode/WasmEncoder.cs](Encode/WasmEncoder.cs) |
| `WasmScanner` | Wasm 扫描器 | [Scanner/WasmScanner.cs](Scanner/WasmScanner.cs) |
| `WasmProtocol` | Wasm 帧协议 | [Scanner/WasmProtocol.cs](Scanner/WasmProtocol.cs) |

## 📚 格式规范参考

- [WebAssembly Binary Format](https://webassembly.github.io/spec/core/binary/index.html)
- [WebAssembly Specification](https://webassembly.github.io/spec/core/)
- [Wasm GitHub](https://github.com/WebAssembly/spec)
