# Acorn.SafeTensors

HuggingFace SafeTensors 模型权重格式的扫描、编码和解码库。

## 格式规范参考

- [SafeTensors Specification](https://huggingface.co/docs/safetensors/index)
- [SafeTensors GitHub](https://github.com/huggingface/safetensors)
- [SafeTensors Format Design](https://huggingface.co/docs/safetensors/safe_tensors)

## 二进制格式结构

SafeTensors 是一种极简、安全、零拷贝的张量存储格式：

```
┌──────────────────────────────────────────┐
│ Header Length (8 bytes, LE uint64)        │
│   N = JSON 头的字节长度                    │
├──────────────────────────────────────────┤
│ JSON Header (N bytes, UTF-8)              │
│   {                                       │
│     "tensor_name": {                      │
│       "dtype": "F32",                     │
│       "shape": [768, 768],               │
│       "data_offsets": [0, 2359296]        │
│     },                                    │
│     "__metadata__": {                     │
│       "format": "pt"                      │
│     }                                     │
│   }                                       │
├──────────────────────────────────────────┤
│ Tensor Data (variable length)             │
│   tensor_0_bytes | tensor_1_bytes | ...   │
└──────────────────────────────────────────┘
```

### 数据类型 (dtype)

| dtype | 字节数/元素 | 说明 |
|---|---|---|
| `BOOL` | 1 | 布尔 |
| `U8` | 1 | 无符号 8 位整数 |
| `I8` | 1 | 有符号 8 位整数 |
| `I16` | 2 | 有符号 16 位整数 |
| `I32` | 4 | 有符号 32 位整数 |
| `I64` | 8 | 有符号 64 位整数 |
| `F16` | 2 | IEEE 754 半精度浮点 |
| `F32` | 4 | IEEE 754 单精度浮点 |
| `F64` | 8 | IEEE 754 双精度浮点 |
| `BF16` | 2 | BFloat16 半精度浮点 |

### 设计特点

- **零拷贝**：张量数据可以直接 mmap 映射，无需反序列化
- **安全**：无 Protobuf/ pickle 反序列化漏洞
- **懒加载**：只需读取 JSON 头即可获取所有元信息
- **确定性**：相同数据编码结果一致

## API

```csharp
using Acorn.SafeTensors.Decode;
using Acorn.SafeTensors.Encode;
using Acorn.SafeTensors.Scanner;

// 解码
var decoder = new SafeTensorsDecoder();
var file = decoder.Decode(File.ReadAllBytes("model.safetensors"));

// 提取单个张量
var tensor = decoder.DecodeTensor(File.ReadAllBytes("model.safetensors"), "encoder.weight");

// 编码
var encoder = new SafeTensorsEncoder();
var tensors = new List<SafeTensorData> { /* ... */ };
var bytes = encoder.Encode(tensors);

// 扫描（仅读取 JSON 头，不加载张量数据）
var scanner = new SafeTensorsScanner(File.ReadAllBytes("model.safetensors"));
var stats = scanner.ScanStatistics();
// stats.TensorCount, stats.TotalParameters, stats.TensorNames, stats.DTypes
```
