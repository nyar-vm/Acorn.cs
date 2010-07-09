# Acorn.Onnx

Open Neural Network Exchange (ONNX) 模型格式的扫描、编码和解码库。

## 格式规范参考

- [ONNX Specification](https://onnx.ai/onnx/intro/)
- [ONNX Protobuf Definition](https://github.com/onnx/onnx/blob/main/onnx/onnx.proto3)
- [ONNX IR Specification](https://onnx.ai/onnx/repo-docs/IR.html)
- [ONNX Operator Schemas](https://onnx.ai/onnx/operators/)

## 二进制格式结构

ONNX 基于 Protocol Buffers 序列化，顶层消息为 `ModelProto`：

```
┌──────────────────────────────────────┐
│ ModelProto (protobuf message)        │
│   ir_version: int64                  │
│   producer_name: string              │
│   producer_version: string           │
│   domain: string                     │
│   model_version: int64               │
│   doc_string: string                 │
│   opset_import: OperatorSetIdProto[] │
│   graph: GraphProto                  │
│   └─ GraphProto                      │
│       node: NodeProto[]              │
│       │  op_type, input[], output[]  │
│       │  attribute[]                 │
│       name: string                   │
│       input: ValueInfoProto[]        │
│       output: ValueInfoProto[]       │
│       initializer: TensorProto[]     │
│       value_info: ValueInfoProto[]   │
└──────────────────────────────────────┘
```

### TensorProto 数据布局

```
TensorProto:
  dims: int64[]           # 张量形状
  data_type: int32        # 数据类型枚举
  segment: SegmentProto   # 可选分段
  float_data: float[]     # F32 数据
  int32_data: int32[]     # I32 数据
  int64_data: int64[]     # I64 数据
  raw_data: bytes         # 原始二进制数据
  name: string            # 张量名称
```

### 数据类型枚举

| 值 | 类型 | 值 | 类型 |
|---|---|---|---|
| 1 | Float | 10 | Float16 |
| 2 | UInt8 | 11 | Double |
| 3 | Int8 | 12 | UInt32 |
| 4 | UInt16 | 13 | UInt64 |
| 5 | Int16 | 16 | BFloat16 |
| 6 | Int32 | 17 | Float8E4M3FN |
| 7 | Int64 | 19 | Float8E5M2 |
| 9 | Bool | | |

## API

```csharp
using Acorn.Onnx.Decode;
using Acorn.Onnx.Encode;
using Acorn.Onnx.Scanner;

// 解码
var decoder = new OnnxDecoder();
var model = decoder.Decode(File.ReadAllBytes("model.onnx"));

// 编码
var encoder = new OnnxEncoder();
var bytes = encoder.Encode(model);

// 扫描
var scanner = new OnnxScanner(File.ReadAllBytes("model.onnx"));
var stats = scanner.ScanStatistics();
```
