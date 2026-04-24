# 🌰 Acorn

Acorn 二进制框架核心库，提供零分配的二进制编解码基础设施。

## 📐 格式布局

Acorn 核心不针对特定二进制格式，而是提供构建任何二进制格式编解码器的基础设施。

### 核心数据类型

| 类型 | 说明 | 对应类 |
|---|---|---|
| `Endianness` | 字节序枚举（小端序/大端序） | [Endianness.cs](Endianness.cs) |
| `FixedBytes4/8/16/32/56/64` | 固定长度字节类型，用于魔数、GUID、哈希等 | [Codec/FixedBytes.cs](Codec/FixedBytes.cs) |

### 编解码器接口

| 接口 | 说明 | 对应类 |
|---|---|---|
| `ICodec<T>` | 泛型类型安全编解码器接口，基于 `Span` 零分配 | [Codec/ICodecT.cs](Codec/ICodecT.cs) |
| `U8/I8` | 8 位整数编解码器 | [Codec/IntegerCodecs.cs](Codec/IntegerCodecs.cs) |
| `U16LE/U16BE/I16LE/I16BE` | 16 位整数编解码器（小端/大端） | [Codec/IntegerCodecs.cs](Codec/IntegerCodecs.cs) |
| `U32LE/U32BE/I32LE/I32BE` | 32 位整数编解码器（小端/大端） | [Codec/IntegerCodecs.cs](Codec/IntegerCodecs.cs) |
| `U64LE/U64BE/I64LE/I64BE` | 64 位整数编解码器（小端/大端） | [Codec/IntegerCodecs.cs](Codec/IntegerCodecs.cs) |
| `F32LE/F32BE/F64LE/F64BE` | 浮点数编解码器（小端/大端） | [Codec/FloatCodecs.cs](Codec/FloatCodecs.cs) |
| `Leb128UInt32/64` | LEB128 无符号变长整数编解码器 | [Codec/VarIntCodecs.cs](Codec/VarIntCodecs.cs) |
| `Leb128Int32/64` | LEB128 有符号变长整数编解码器 | [Codec/VarIntCodecs.cs](Codec/VarIntCodecs.cs) |
| `ZigZagLeb128Int32/64` | ZigZag + LEB128 有符号变长整数编解码器 | [Codec/VarIntCodecs.cs](Codec/VarIntCodecs.cs) |

### 帧协议基础设施

| 类型 | 说明 | 对应类 |
|---|---|---|
| `IFrameProtocol` | 协议帧接口，定义消息边界识别方式 | [Frame/IFrameProtocol.cs](Frame/IFrameProtocol.cs) |
| `Frame` | 协议帧数据结构，`ref struct` 零拷贝 | [Frame/Frame.cs](Frame/Frame.cs) |
| `FrameScanner<TProtocol>` | 泛型帧扫描器，编译期绑定协议实现 | [Frame/FrameScanner.cs](Frame/FrameScanner.cs) |
| `AlgebraicFrame<TKind, TProtocol>` | 代数帧类型，一帧可能是多种类型之一 | [Frame/AlgebraicFrame.cs](Frame/AlgebraicFrame.cs) |
| `ByteBuffer` | 零拷贝内存缓冲区，基于 `ReadOnlySpan` | [Frame/ByteBuffer.cs](Frame/ByteBuffer.cs) |
| `ByteBufferWriter` | 字节缓冲区写入器 | [Frame/ByteBufferWriter.cs](Frame/ByteBufferWriter.cs) |
| `FrameBuilder` | 帧构建器 | [Frame/FrameBuilder.cs](Frame/FrameBuilder.cs) |
| `AsyncFrameScanner` | 异步帧扫描器 | [Frame/AsyncFrameScanner.cs](Frame/AsyncFrameScanner.cs) |
| `IFrameKind` | 帧类型标识接口 | [Frame/IFrameKind.cs](Frame/IFrameKind.cs) |

### 源生成器特性

| 特性 | 说明 | 对应类 |
|---|---|---|
| `[BinarySerializable]` | 标记结构体为二进制可序列化，生成 TryRead/WriteTo | [Attributes/BinarySerializableAttribute.cs](Attributes/BinarySerializableAttribute.cs) |
| `[Field]` | 标记字段序列化顺序和编码方式 | [Attributes/FieldAttribute.cs](Attributes/FieldAttribute.cs) |
| `[BitField]` | 位域特性 | [Attributes/BitFieldAttribute.cs](Attributes/BitFieldAttribute.cs) |
| `[OffsetTable]` | 偏移表特性 | [Attributes/OffsetTableAttribute.cs](Attributes/OffsetTableAttribute.cs) |
| `[AlgebraicUnion]` | 代数联合特性 | [Attributes/AlgebraicUnionAttribute.cs](Attributes/AlgebraicUnionAttribute.cs) |
| `[ArrayLengthFrom]` | 数组长度来源特性 | [Attributes/ArrayLengthFromAttribute.cs](Attributes/ArrayLengthFromAttribute.cs) |

### 流处理

| 类型 | 说明 | 对应类 |
|---|---|---|
| `BitStream` | 位流读取器 | [Stream/BitStream.cs](Stream/BitStream.cs) |
