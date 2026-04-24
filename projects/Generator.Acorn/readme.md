# 📝 Generator.Acorn

Acorn 二进制序列化源生成器，为标记 `[BinarySerializable]` 的 `partial struct` 自动生成 `TryRead` 和 `WriteTo` 方法。

## 📐 生成代码结构

### 生成的文件

| 文件 | 说明 |
|---|---|
| `{Namespace}.{StructName}.g.cs` | 为每个标记的结构体生成的部分类文件 |

### 生成的成员

| 成员 | 说明 |
|---|---|
| `TryRead(ref ByteBuffer buffer, out {StructName} value)` | 从缓冲区读取结构体 |
| `WriteTo(ref ByteBufferWriter writer)` | 将结构体写入缓冲区 |
| `GetSize()` | 获取序列化后的大小 |
| `Match(...)` | 代数联合类型的匹配方法（如适用） |
| `BitField` 属性 | 位域属性的 getter/setter（如适用） |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `BinarySerializableGenerator` | 增量源生成器入口 | [BinarySerializableGenerator.cs](BinarySerializableGenerator.cs) |
| `StructSerializationInfo` | 结构体序列化信息 | [StructSerializationInfo.cs](StructSerializationInfo.cs) |
| `FieldSerializationInfo` | 字段序列化信息 | [FieldSerializationInfo.cs](FieldSerializationInfo.cs) |
| `FieldTypeKind` | 字段类型枚举 | [FieldTypeKind.cs](FieldTypeKind.cs) |
| `BitFieldInfo` | 位域信息 | [BitFieldInfo.cs](BitFieldInfo.cs) |
| `OffsetTableInfo` | 偏移表信息 | [OffsetTableInfo.cs](OffsetTableInfo.cs) |
| `AlgebraicUnionInfo` | 代数联合信息 | [AlgebraicUnionInfo.cs](AlgebraicUnionInfo.cs) |
| `AlgebraicUnionCase` | 代数联合案例 | [AlgebraicUnionCase.cs](AlgebraicUnionCase.cs) |

### 支持的特性

| 特性 | 说明 |
|---|---|
| `[BinarySerializable]` | 标记结构体为可二进制序列化，支持 `Endianness` 参数 |
| `[Field]` | 标记字段，支持 `Order`、`Length`、`ConditionalOn` 等参数 |
| `[BitField]` | 标记位域字段 |
| `[OffsetTable]` | 标记偏移表字段 |
| `[AlgebraicUnion]` | 标记代数联合类型 |
| `[ArrayLengthFrom]` | 指定数组长度来源字段 |

### 支持的字段类型

| 类型 | 说明 |
|---|---|
| 基础类型 | `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool` |
| 固定字节 | `FixedBytes4/8/16/32/56/64` |
| 字符串 | `string`（长度前缀） |
| 数组 | `T[]`（需指定长度来源） |
| 嵌套结构体 | 其他 `[BinarySerializable]` 结构体 |
| 枚举 | 基于整数类型的枚举 |

## 📚 参考文档

- [C# Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Incremental Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
