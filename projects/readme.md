# 🔌 接入新二进制格式指南

本文档指导如何为 Acorn Binary Framework 接入一个新的二进制格式。

## 📋 目录

1. [评估需求](#1-评估需求)
2. [创建项目结构](#2-创建项目结构)
3. [实现编解码器](#3-实现编解码器)
4. [定义数据结构](#4-定义数据结构)
5. [实现帧协议（可选）](#5-实现帧协议可选)
6. [编写测试](#6-编写测试)
7. [编写文档](#7-编写文档)
8. [发布到 NuGet](#8-发布到-nuget)

---

## 1. 评估需求

在开始之前，评估你的二进制格式属于哪种类型：

| 类型 | 特征 | 推荐方案 |
|---|---|---|
| **固定结构** | 字段顺序固定，长度固定或由长度字段决定 | 纯特性声明 |
| **条件结构** | 字段存在与否取决于其他字段的值 | 特性 + `ConditionalOn` |
| **偏移表结构** | 文件头部存储偏移量，指向其他数据块 | 特性 + `OffsetTableAttribute` |
| **帧流协议** | 数据流由多个帧组成，需要切帧 | 实现 `IFrameProtocol` |
| **指令流** | 操作码 + 操作数的指令序列 | 实现 `IFrameProtocol` + 代数帧 |
| **自描述格式** | 字段标签 + 值，类似 Protobuf | 实现 `IFrameProtocol` + 自定义特性 |

---

## 2. 创建项目结构

### 2.1 创建项目目录

```bash
mkdir -p projects/Acorn.YourFormat/{Data,Decode,Encode,Scanner}
```

### 2.2 创建项目文件

创建 `projects/Acorn.YourFormat/Acorn.YourFormat.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Acorn.YourFormat</RootNamespace>
    <AssemblyName>Acorn.YourFormat</AssemblyName>
    <PackageId>Acorn.YourFormat</PackageId>
    <Version>0.1.0</Version>
    <Authors>Your Name</Authors>
    <Description>YourFormat 二进制格式编解码器</Description>
    <PackageProjectUrl>https://github.com/nyar-vm/Acorn.cs</PackageProjectUrl>
    <RepositoryUrl>https://github.com/nyar-vm/Acorn.cs</RepositoryUrl>
    <PackageLicenseExpression>MPL-2.0</PackageLicenseExpression>
    <PackageTags>acorn;binary;yourformat</PackageTags>
    <PackageReadmeFile>readme.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Acorn\Acorn.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="readme.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

### 2.3 推荐目录结构

```
Acorn.YourFormat/
├── Acorn.YourFormat.csproj
├── readme.md
├── Data/
│   ├── YourFormatConstants.cs    # 常量定义（魔数、版本等）
│   └── YourFormatData.cs         # 数据结构定义
├── Decode/
│   └── YourFormatDecoder.cs      # 解码器
├── Encode/
│   └── YourFormatEncoder.cs      # 编码器
└── Scanner/
    └── YourFormatScanner.cs      # 扫描器（可选）
```

---

## 3. 实现编解码器

### 3.1 简单场景：纯特性声明

如果格式是固定结构，只需定义数据结构：

```csharp
// Data/YourFormatData.cs
using Acorn;
using Acorn.Attributes;

namespace Acorn.YourFormat;

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct YourFormatHeader
{
    [Field(Order = 0, Length = 4)]
    public FixedBytes4 Magic;

    [Field(Order = 1)]
    public uint Version;

    [Field(Order = 2)]
    public uint DataLength;

    [Field(Order = 3, LengthField = nameof(DataLength))]
    public byte[] Data;
}
```

源生成器会自动生成 `TryRead` 和 `WriteTo` 方法。

### 3.2 复杂场景：自定义编解码器

如果需要自定义编解码逻辑，实现 `ICodec<T>`：

```csharp
// Codec/YourCustomCodec.cs
using Acorn;
using Acorn.Codec;

namespace Acorn.YourFormat;

public struct YourCustomCodec : ICodec<YourCustomType>
{
    public int GetSize(YourCustomType value)
    {
        return 8;
    }

    public void Encode(YourCustomType value, Span<byte> destination)
    {
        var encoder = new YourFormatEncoder(destination);
        encoder.Encode(value);
    }

    public YourCustomType Decode(ReadOnlySpan<byte> source)
    {
        var decoder = new YourFormatDecoder(source);
        return decoder.Decode();
    }
}
```

---

## 4. 定义数据结构

### 4.1 基本数据结构

```csharp
// Data/YourFormatData.cs
using Acorn;
using Acorn.Attributes;

namespace Acorn.YourFormat;

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct YourFormatFile
{
    [Field(Order = 0)]
    public YourFormatHeader Header;

    [Field(Order = 1)]
    public uint SectionCount;

    [Field(Order = 2, LengthField = nameof(SectionCount))]
    public SectionEntry[] Sections;
}

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct SectionEntry
{
    [Field(Order = 0, Length = 32)]
    public FixedBytes32 Name;

    [Field(Order = 1)]
    public uint Offset;

    [Field(Order = 2)]
    public uint Size;
}
```

### 4.2 条件字段

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct YourFormatPacket
{
    [Field(Order = 0)]
    public byte Flags;

    [Field(Order = 1, ConditionalOn = nameof(HasExtendedHeader))]
    public ExtendedHeader Extended;

    [Field(Order = 2)]
    public byte[] Payload;

    public bool HasExtendedHeader => (Flags & 0x80) != 0;
}
```

### 4.3 位域

```csharp
[BinarySerializable]
public partial struct YourFormatFlags
{
    [Field(Order = 0)]
    public byte Raw;

    [BitField(BitOffset = 0, BitCount = 1)]
    public bool IsValid;

    [BitField(BitOffset = 1, BitCount = 3)]
    public byte Type;

    [BitField(BitOffset = 4, BitCount = 4)]
    public byte Priority;
}
```

### 4.4 偏移表

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct YourFormatContainer
{
    [Field(Order = 0)]
    public uint Magic;

    [Field(Order = 1)]
    public uint TableOffset;

    [OffsetTable(OffsetField = nameof(TableOffset), TargetType = typeof(DataTable))]
    public DataTable Table;
}
```

---

## 5. 实现帧协议（可选）

如果格式是基于帧的流式协议，实现 `IFrameProtocol`：

### 5.1 定义帧协议

```csharp
// Scanner/YourFormatProtocol.cs
using Acorn;
using Acorn.Frame;

namespace Acorn.YourFormat;

public struct YourFormatProtocol : IFrameProtocol
{
    public int MinFrameSize => 4;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (buffer.Length < 4)
        {
            frameSize = 0;
            return false;
        }

        frameSize = BitConverter.ToInt32(buffer.Slice(0, 4)) + 4;
        return buffer.Length >= frameSize;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        frame = new Frame(size, buffer.Slice(4, size - 4));
        return true;
    }
}
```

### 5.2 定义代数帧

```csharp
// Data/YourFormatFrame.cs
using Acorn;
using Acorn.Attributes;

namespace Acorn.YourFormat;

public enum FrameType : byte
{
    Data = 0x01,
    Ack = 0x02,
    Error = 0xFF
}

[BinarySerializable(Endianness = Endianness.Little)]
[AlgebraicUnion(
    DiscriminatorField = nameof(Type),
    Cases = new[] { typeof(DataFrame), typeof(AckFrame), typeof(ErrorFrame) }
)]
public partial struct YourFormatFrame
{
    [Field(Order = 0)]
    public FrameType Type;

    [Field(Order = 1, Optional = true)]
    public DataFrame Data;

    [Field(Order = 2, Optional = true)]
    public AckFrame Ack;

    [Field(Order = 3, Optional = true)]
    public ErrorFrame Error;
}

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct DataFrame
{
    [Field(Order = 0)]
    public uint Sequence;

    [Field(Order = 1)]
    public ushort Length;

    [Field(Order = 2, LengthField = nameof(Length))]
    public byte[] Payload;
}
```

### 5.3 使用帧扫描器

```csharp
// Scanner/YourFormatScanner.cs
using Acorn;
using Acorn.Frame;

namespace Acorn.YourFormat;

public ref struct YourFormatScanner
{
    private FrameScanner<YourFormatProtocol> _scanner;

    public YourFormatScanner(ByteBuffer buffer)
    {
        _scanner = new FrameScanner<YourFormatProtocol>(buffer);
    }

    public bool TryReadNext(out YourFormatFrame frame)
    {
        if (!_scanner.TryReadNext(out var rawFrame))
        {
            frame = default;
            return false;
        }

        frame = YourFormatFrame.FromFrame(rawFrame);
        return true;
    }
}
```

---

## 6. 编写测试

### 6.1 创建测试项目

```bash
mkdir -p tests/Acorn.YourFormat.Tests
```

### 6.2 测试示例

```csharp
// tests/Acorn.YourFormat.Tests/YourFormatTests.cs
using Acorn;
using Acorn.YourFormat;
using Xunit;

namespace Acorn.YourFormat.Tests;

public class YourFormatTests
{
    [Fact]
    public void Decode_Encode_RoundTrip()
    {
        var original = new YourFormatHeader
        {
            Magic = new FixedBytes4("YFMT"),
            Version = 1,
            DataLength = 4,
            Data = new byte[] { 0x01, 0x02, 0x03, 0x04 }
        };

        var buffer = new byte[256];
        var writer = new ByteBufferWriter(buffer);
        original.WriteTo(ref writer);

        var reader = new ByteBuffer(buffer);
        Assert.True(YourFormatHeader.TryRead(ref reader, out var decoded));

        Assert.Equal(original.Magic, decoded.Magic);
        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(original.DataLength, decoded.DataLength);
        Assert.Equal(original.Data, decoded.Data);
    }

    [Fact]
    public void Scanner_ReadsMultipleFrames()
    {
        var data = BuildTestFrames();
        var buffer = new ByteBuffer(data);
        var scanner = new YourFormatScanner(buffer);

        var count = 0;
        while (scanner.TryReadNext(out var frame))
        {
            count++;
        }

        Assert.Equal(3, count);
    }

    private static byte[] BuildTestFrames()
    {
        var result = new List<byte>();

        for (var i = 0; i < 3; i++)
        {
            var payload = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) };
            result.AddRange(BitConverter.GetBytes(payload.Length));
            result.AddRange(payload);
        }

        return result.ToArray();
    }
}
```

---

## 7. 编写文档

### 7.1 创建 README

创建 `projects/Acorn.YourFormat/readme.md`：

```markdown
# 📦 Acorn.YourFormat

YourFormat 二进制格式编解码器。

## 特性

- ✅ 支持完整 YourFormat 规范
- ✅ 零分配解码
- ✅ 流式帧扫描
- ✅ 代数帧匹配

## 安装

\`\`\`bash
dotnet add package Acorn.YourFormat
\`\`\`

## 快速开始

### 解码文件

\`\`\`csharp
using Acorn;
using Acorn.YourFormat;

var data = File.ReadAllBytes("file.yfmt");
var buffer = new ByteBuffer(data);

if (YourFormatFile.TryRead(ref buffer, out var file))
{
    Console.WriteLine($"Version: {file.Header.Version}");
}
\`\`\`

### 帧扫描

\`\`\`csharp
var scanner = new YourFormatScanner(buffer);
while (scanner.TryReadNext(out var frame))
{
    frame.Match(
        data => Console.WriteLine($"Data: {data.Sequence}"),
        ack => Console.WriteLine($"Ack: {ack.Sequence}"),
        error => Console.WriteLine($"Error: {error.Code}")
    );
}
\`\`\`

## 格式规范

| 字段 | 偏移 | 大小 | 说明 |
|---|---|---|---|
| Magic | 0x00 | 4 | 魔数 "YFMT" |
| Version | 0x04 | 4 | 版本号 |
| DataLength | 0x08 | 4 | 数据长度 |
| Data | 0x0C | N | 数据内容 |

## 许可证

MPL-2.0
```

---

## 8. 发布到 NuGet

### 8.1 更新 vendor 包

在 `vendors/Nyar.Vendor.Acorn/Nyar.Vendor.Acorn.csproj` 中添加新项目引用：

```xml
<ProjectReference Include="..\..\projects\Acorn.YourFormat\Acorn.YourFormat.csproj" />
```

### 8.2 打包

```bash
cd projects/Acorn.YourFormat
dotnet pack -c Release
```

### 8.3 发布

```bash
dotnet nuget push bin/Release/Acorn.YourFormat.0.1.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## 📚 参考文档

- [架构设计](../documentation/architecture.md) - 核心接口详解
- [特性系统](../documentation/attributes.md) - 声明式特性
- [扩展点](../documentation/extensibility.md) - 自定义扩展
- [典型用例](../documentation/examples.md) - 现有格式实现参考

---

## 🤝 贡献指南

1. Fork 仓库
2. 创建特性分支 (`git checkout -b feature/add-yourformat`)
3. 提交更改 (`git commit -m '✨ 添加 YourFormat 支持'`)
4. 推送到分支 (`git push origin feature/add-yourformat`)
5. 创建 Pull Request

请确保：
- ✅ 代码通过所有测试
- ✅ 遵循项目代码规范
- ✅ 添加了完整的文档
- ✅ 更新了 vendor 包引用
