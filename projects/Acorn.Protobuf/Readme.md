# Acorn.Protobuf

Acorn Protobuf 格式库，提供 Protocol Buffers 消息的扫描、编码和解码功能，**无需 proto 文件即可解析 Protobuf 消息**，更轻量更高效。

## 核心特性

- **无需 Proto 文件**：直接解析 Protobuf 二进制消息，无需预编译 .proto 文件
- **轻量高效**：最小化依赖，专注于核心编解码功能，性能优异
- **Protobuf 消息解码**：支持解析任意 Protobuf 二进制消息
- **Protobuf 消息编码**：支持生成 Protobuf 二进制消息
- **Varint 编码**：实现 Protobuf 特有的可变长度整数编码
- **ZigZag 编码**：支持有符号整数的高效编码
- **长度前缀字符串**：支持 Protobuf 字符串编码
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口，保持一致性

## 为什么选择 Acorn.Protobuf？

- **无依赖**：不依赖 Google.Protobuf 库，减小应用体积
- **高性能**：优化的编解码算法，比传统实现更快
- **灵活**：支持动态解析未知结构的 Protobuf 消息
- **易于集成**：与 Acorn 生态系统无缝集成
- **类型安全**：提供强类型的数据结构

## 安装

```bash
dotnet add package Acorn.Protobuf
```

## 使用示例

### 解码 Protobuf 消息（无需 Proto 文件）

```csharp
using System.IO;
using Acorn.Protobuf.Decode;

// Protobuf 二进制数据
var data = new byte[] { 0x08, 0x2A, 0x12, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };

using var stream = new MemoryStream(data);
using var reader = new BinaryReader(stream);

// 直接解码，无需 proto 文件
var protobufDecoder = new ProtobufDecoder(reader);
var message = protobufDecoder.DecodeMessage();

Console.WriteLine($"Message name: {message.Name}");
Console.WriteLine($"Fields count: {message.Fields.Count}");
foreach (var field in message.Fields)
{
    Console.WriteLine($"Field {field.FieldNumber}: {field.Type} - {field.Name}");
}
```

### 编码 Protobuf 消息

```csharp
using System.IO;
using Acorn.Protobuf.Encode;
using Acorn.Protobuf.Data;

var message = new ProtobufMessageData
{
    Name = "TestMessage",
    Fields = new List<ProtobufFieldData>
    {
        new ProtobufFieldData
        {
            FieldNumber = 1,
            Type = "varint",
            Name = "id"
        },
        new ProtobufFieldData
        {
            FieldNumber = 2,
            Type = "length_delimited",
            Name = "name"
        }
    }
};

using var stream = new MemoryStream();
using var writer = new BinaryWriter(stream);

var protobufEncoder = new ProtobufEncoder(writer);
protobufEncoder.EncodeMessage(message);

var data = stream.ToArray();
Console.WriteLine($"Encoded data length: {data.Length}");
```

### 使用 Protobuf 编解码器

```csharp
using System.IO;
using Acorn.Codec.Byte;
using Acorn.Protobuf.Codec;

// 使用 Protobuf Varint 编解码器
var varintCodec = new ProtobufVarintCodec();

// 使用 Protobuf 字符串编解码器
var stringCodec = new ProtobufStringCodec();

// 示例：编码和解码整数
using var stream = new MemoryStream();
using var writer = new BinaryWriter(stream);
using var reader = new BinaryReader(stream);

// 编码
varintCodec.WriteI32(writer, 42);

// 解码
stream.Position = 0;
var value = varintCodec.ReadI32(reader);
Console.WriteLine($"Encoded value: {value}");
```

## 项目结构

- `Acorn.Protobuf/`
  - `Codec/` - Protobuf 编解码器实现
    - `ProtobufVarintCodec.cs` - Varint 编解码器
    - `ProtobufStringCodec.cs` - 字符串编解码器
  - `Data/` - Protobuf 数据结构
    - `ProtobufConstants.cs` - Protobuf 常量定义
    - `ProtobufMessageData.cs` - 消息数据结构
  - `Decode/` - Protobuf 解码器
    - `ProtobufDecoder.cs` - 消息解码器
  - `Encode/` - Protobuf 编码器
    - `ProtobufEncoder.cs` - 消息编码器
  - `Scanner/` - Protobuf 扫描器
    - `ProtobufScanner.cs` - 消息结构扫描器

## 性能优势

- **内存占用低**：无需加载和解析 .proto 文件
- **启动速度快**：没有运行时反射开销
- **编码效率高**：优化的 Varint 和 ZigZag 编码实现
- **解码速度快**：直接解析二进制数据，无需中间表示

## 应用场景

- **网络协议解析**：解析来自网络的 Protobuf 消息
- **文件格式处理**：处理 Protobuf 格式的配置文件或数据文件
- **跨语言通信**：与其他语言的 Protobuf 实现交互
- **嵌入式系统**：在资源受限的环境中使用
- **快速原型开发**：无需编写和编译 .proto 文件，快速测试 Protobuf 消息

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
