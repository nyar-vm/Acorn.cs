# 🔌 接入新二进制格式指南

本文档指导如何为 Acorn Binary Framework 接入一个新的二进制格式，并详细记录所有已实现项目的功能规范。

## 📋 目录

1. [评估需求](#1-评估需求)
2. [创建项目结构](#2-创建项目结构)
3. [实现编解码器](#3-实现编解码器)
4. [定义数据结构](#4-定义数据结构)
5. [实现帧协议（可选）](#5-实现帧协议可选)
6. [编写测试](#6-编写测试)
7. [编写文档](#7-编写文档)
8. [发布到 NuGet](#8-发布到-nuget)
9. [已实现项目规范](#9-已实现项目规范)

***

## 1. 评估需求

在开始之前，评估你的二进制格式属于哪种类型：

| 类型        | 特征                   | 推荐方案                        |
| --------- | -------------------- | --------------------------- |
| **固定结构**  | 字段顺序固定，长度固定或由长度字段决定  | 纯特性声明                       |
| **条件结构**  | 字段存在与否取决于其他字段的值      | 特性 + `ConditionalOn`        |
| **偏移表结构** | 文件头部存储偏移量，指向其他数据块    | 特性 + `OffsetTableAttribute` |
| **帧流协议**  | 数据流由多个帧组成，需要切帧       | 实现 `IFrameProtocol`         |
| **指令流**   | 操作码 + 操作数的指令序列       | 实现 `IFrameProtocol` + 代数帧   |
| **自描述格式** | 字段标签 + 值，类似 Protobuf | 实现 `IFrameProtocol` + 自定义特性 |

***

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

***

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

***

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

***

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

***

## 6. 编写测试

### 6.1 创建测试项目

在 `d:\RiderProjects\Acorn.cs\examples` 目录下创建测试项目：

```bash
mkdir -p examples/Acorn.YourFormat.Tests
```

创建 `examples/Acorn.YourFormat.Tests/Acorn.YourFormat.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net11.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <RootNamespace>Acorn.YourFormat.Tests</RootNamespace>
        <PackageId>nyar-vm.Acorn.YourFormat.Tests</PackageId>
        <Version>0.0.0</Version>
        <Authors>nyar-vm</Authors>
        <Description>YourFormat 二进制解析测试</Description>
        <PackageTags>test;yourformat;acorn</PackageTags>
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    </PropertyGroup>
    <ItemGroup>
        <ProjectReference Include="../../projects/Acorn.YourFormat/Acorn.YourFormat.csproj" />
        <ProjectReference Include="../Acorn.Tests/Acorn.Tests.csproj" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="xunit" Version="2.9.3" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
          <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

### 6.2 测试示例

```csharp
// examples/Acorn.YourFormat.Tests/YourFormatTests.cs
using Acorn;
using Acorn.YourFormat;
using Acorn.Tests.TestUtils;
using Xunit;

namespace Acorn.YourFormat.Tests;

public class YourFormatTests : DecoderTestBase, EncoderTestBase
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

***

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

***

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

***

## 9. 已实现项目规范

### 9.1 项目分类

| 分类 | 项目 | 说明 |
|:---|:---|:---|
| 可执行文件格式 | COFF, ELF, MachO, PE | 操作系统原生二进制格式 |
| 调试信息格式 | DWARF | 调试信息标准 |
| 编译器中间格式 | LLVM | LLVM Bitcode 中间表示 |
| 3D 模型格式 | Gltf | Khronos glTF/GLB 3D 模型 |
| 2D 动画格式 | Live2D, Spine | 参数化 2D 角色动画 |
| 图像格式 | Psd | Adobe Photoshop 文档 |
| 机器学习格式 | Onnx, SafeTensors | ONNX 模型 / HuggingFace 张量 |
| 数据库协议 | MySql, PostgreSql | 关系型数据库网络协议 |
| 消息协议 | Redis, ZeroMQ, Protobuf | RESP / ZMTP / Protobuf Wire Format |
| 文档格式 | Office | MS Office 二进制格式（XLS/DOC/PPT） |
| 着色器格式 | SpirV | Khronos SPIR-V 着色器中间语言 |
| Web 格式 | Wasm | WebAssembly 二进制格式 |
| 压缩格式 | Zip | ZIP 归档格式 |

### 9.2 实现模式说明

| 模式 | 标记 | 说明 |
|:---|:---|:---|
| 声明式 | `[声明式]` | 使用 `[BinarySerializable]` + `[Field]` 特性，源生成器自动生成 `TryRead`/`WriteTo` |
| 手动解码 | `[手动解码]` | 使用 `ByteBuffer` 手动读取字段 |
| 手动编码 | `[手动编码]` | 使用 `ByteBufferWriter` 手动写入字段 |
| 帧协议 | `[帧协议]` | 实现 `IFrameProtocol` 接口，使用 `FrameScanner<T>` |
| 扫描器接口 | `[扫描器接口]` | 定义 `IXxxScanner` 接口 |
| 自定义 Codec | `[自定义Codec]` | 实现 `ICodec<T>` 接口 |

### 9.2.1 项目元数据

所有项目均目标 `net11.0` 框架，版本 `0.0.0`，许可证 `MPL-2.0`。

| 项目 | RootNamespace | 说明 | Acorn 依赖 |
|:---|:---|:---|:---|
| COFF | `Acorn.Coff` | Windows 目标文件（.obj） | Acorn |
| DWARF | `Acorn.DWARF` | 调试信息（.debug） | Acorn |
| ELF | `Acorn.ELF` | Linux 可执行文件（.elf, .so） | Acorn |
| Gltf | `Acorn.Gltf` | Khronos GLTF / GLB 模型 | Acorn |
| Live2D | `Acorn.Live2D` | Live2D Cubism 模型 | Acorn |
| LLVM | `Acorn.LLVM` | LLVM Bitcode（.bc） | Acorn |
| MachO | `Acorn.MachO` | macOS/iOS 可执行文件（.macho, .dylib） | Acorn |
| MySql | `Acorn.MySql` | MySQL 协议 | Acorn |
| Office | `Acorn.Office` | Office 97-2003（.xls, .doc, .ppt） | Acorn, **Acorn.Zip** |
| Onnx | `Acorn.Onnx` | ONNX 模型 | Acorn, **Acorn.Protobuf** |
| PE | `Acorn.Pe` | Windows 可执行文件（.exe, .dll） | Acorn, **Acorn.COFF** |
| PostgreSql | `Acorn.PostgreSql` | PostgreSQL 协议 | Acorn |
| Protobuf | `Acorn.Protobuf` | Protocol Buffers 消息 | Acorn |
| Psd | `Acorn.Psd` | Adobe Photoshop PSD | Acorn |
| Redis | `Acorn.Redis` | Redis 协议 | Acorn |
| SafeTensors | `Acorn.SafeTensors` | HuggingFace SafeTensors 权重 | Acorn |
| Spine | `Acorn.Spine` | Spine 2D 骨骼动画 | Acorn |
| SpirV | `Acorn.Spirv` | Khronos SPIR-V 着色器 | Acorn |
| Wasm | `Acorn.Wasm` | WebAssembly 二进制 | Acorn |
| ZeroMQ | `Acorn.ZeroMQ` | ZeroMQ 协议 | Acorn |
| Zip | `Acorn.Zip` | ZIP 压缩包 | Acorn |

**跨项目依赖关系**：

```
Acorn.PE ──→ Acorn.COFF      （PE 格式内部包含 COFF 目标文件）
Acorn.Office ──→ Acorn.Zip    （Office OpenXml 解码委托 ZipDecoder）
Acorn.Onnx ──→ Acorn.Protobuf （ONNX 模型基于 Protobuf 序列化）
```

***

### 9.3 可执行文件格式

#### Acorn.COFF — Windows 目标文件格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `CoffHeaderData` | `[BinarySerializable]` partial struct | COFF 文件头（Machine, NumberOfSections, TimeDateStamp 等） |
| `CoffSectionHeaderData` | `[BinarySerializable]` partial struct | 节区头（NameBytes `FixedBytes8`, PhysicalAddress, VirtualAddress 等） |
| `CoffRelocationData` | `[BinarySerializable]` partial struct | 重定位条目 |
| `CoffSymbolData` | sealed class | 符号表条目（Name, Value, SectionNumber, Type, StorageClass） |
| `CoffFileData` | sealed class | 完整 COFF 文件（Header, Sections, Symbols, Relocations） |
| `CoffDecoder` | sealed class | 手动解码，读取 COFF 头 → 节区头 → 符号表 → 重定位（数据结构定义了 `[BinarySerializable]` 但解码器未使用 `TryRead`） |
| `CoffScanner` | class (static `Scan`) | 文本扫描结果，委托 `CoffDecoder` 解码后输出机器类型、节区列表、符号列表 |

#### Acorn.ELF — Linux 可执行文件格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `ELFHeaderData` | sealed class | ELF 头（Magic, Class, DataEncoding, Type, Machine, EntryPoint 等），含 `Is64Bit`/`IsLittleEndian` 属性 |
| `ELFSectionHeaderData` | sealed class | 节区头（NameIndex, Type, Flags, Address, Offset, Size 等） |
| `ELFProgramHeaderData` | sealed class | 程序头（Type, Flags, Offset, VirtualAddress, FileSize, MemorySize 等） |
| `ELFFileData` | sealed class | 完整 ELF 文件（Header, SectionHeaders, ProgramHeaders），含 `IsExecutable`/`IsSharedLibrary` |
| `ELFDecoder` | sealed class | 手动解码，支持 32/64 位和大小端，读取 ELF 头 → 节区头 → 程序头 |
| `ELFScanner` | class (static `Scan`) | 文本扫描结果，输出文件类型、架构、字节序、节区/段列表、入口点 |

#### Acorn.MachO — macOS/iOS 可执行文件格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `MachOHeaderData` | sealed class | Mach-O 头（Magic, CPUType, CPUSubtype, FileType, NumberOfLoadCommands 等），含 `Is64Bit`/`IsExecutable`/`IsDynamicLibrary` |
| `MachOLoadCommandData` | sealed class | 加载命令（Command, Size, Data） |
| `MachOSectionData` | sealed class | 节区（SectionName, SegmentName, Address, Size, Offset 等） |
| `MachOFileData` | sealed class | 完整 Mach-O 文件（Header, LoadCommands, Sections） |
| `MachODecoder` | sealed class | 手动解码，自动检测大小端，读取 Mach-O 头 → 加载命令 → 节区 |
| `MachOScanner` | class (static `Scan`) | 文本扫描结果，输出文件类型、架构、CPU 类型、加载命令、节区列表 |

#### Acorn.PE — Windows 可执行文件格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `PeSectionData` | `[BinarySerializable]` partial struct | PE 节区（NameBytes `FixedBytes8`, VirtualSize, VirtualAddress, SizeOfRawData 等） |
| `PeHeaderData` | sealed class | PE 头（DosMagic, PeHeaderOffset, PeMagic, Machine, NumberOfSections 等） |
| `PeOptionalHeaderData` | sealed class | 可选头（Magic, ImageBase, SectionAlignment, AddressOfEntryPoint, Subsystem 等） |
| `PeFileData` | sealed class | 完整 PE 文件（Header, OptionalHeader, Sections），含 `IsDll`/`IsExecutable`/`Is64Bit` |
| `PeDecoder` | sealed class | 手动解码，读取 DOS 头 → PE 头 → 可选头 → 节区表，支持 PE32/PE32+ |
| `PeScanner` | class (static `Scan`) | 文本扫描结果，输出文件类型、架构、机器类型、节区列表、可选头信息 |

***

### 9.4 调试/编译器格式

#### Acorn.DWARF — 调试信息格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `DWARFCompilationUnitData` | sealed class | 编译单元（UnitLength, Version, DebugInfoOffset, AddressSize, Entries） |
| `DWARFEntryData` | sealed class | DIE 条目（AbbreviationCode, Tag, HasChildren, Attributes） |
| `DWARFAttributeData` | sealed class | 属性（Name, Form, Value） |
| `DWARFLineNumberTableData` | sealed class | 行号表（UnitLength, Version, MinimumInstructionLength, OpcodeBase, FileNames 等） |
| `DWARFFileData` | sealed class | 完整 DWARF 文件（CompilationUnits, LineNumberTables） |
| `DWARFDecoder` | sealed class | 手动解码，使用 LEB128 读取编译单元、条目和属性值 |
| `DWARFScanner` | class (static `Scan`) | 文本扫描结果，输出编译单元数量、标签类型、行号表数量 |

#### Acorn.LLVM — LLVM Bitcode 格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[手动解码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `LLVMMagicData` | sealed class | 魔数和标识（Magic, Version, IsWrapped） |
| `LLVMBlockData` | sealed class | Bitcode 块（BlockID, BlockSize, SubBlocks, Records） |
| `LLVMRecordData` | sealed class | Bitcode 记录（Code, Operands） |
| `LLVMModuleData` | sealed class | 模块（ModuleID, TargetTriple, DataLayout, Functions, Globals） |
| `LLVMFunctionData` | sealed class | 函数（Name, ReturnType, Parameters, BasicBlockCount, InstructionCount） |
| `LLVMGlobalData` | sealed class | 全局变量（Name, Type, IsConstant, Linkage） |
| `LLVMBitcodeData` | sealed class | 完整 Bitcode 文件（Magic, TopLevelBlocks, Modules） |
| `LLVMDecoder` | sealed class | 手动解码，使用 LEB128 读取块和记录，递归解析子块 |
| `LLVMScanner` | class (static `Scan`) | 文本扫描结果，递归输出块结构 |

***

### 9.5 3D/2D 模型格式

#### Acorn.Gltf — Khronos glTF/GLB 3D 模型格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `GltfConstants` | static class | 常量（GlbMagicNumber, GlbVersion, ChunkTypeJson, ChunkTypeBin），枚举（`GltfFilterMode`, `GltfWrapMode`, `GltfPrimitiveMode`） |
| `GlbHeader` | `[BinarySerializable]` partial struct | GLB 文件头（Magic `FixedBytes4`, Version, Length） |
| `GlbChunkHeader` | `[BinarySerializable]` partial struct | GLB 块头（Length, Type） |
| `GltfModelData` | sealed class | 完整 glTF 模型（Asset, Scenes, Nodes, Meshes, Buffers, Materials, Textures, Animations 等） |
| `GltfAsset` / `GltfScene` / `GltfNode` / `GltfMesh` / ... | sealed class | 完整 glTF 2.0 规范数据结构 |
| `GltfDecoder` | sealed class | 支持 JSON 和 GLB 两种输入，GLB 使用 `GlbHeader.TryRead` 解码文件头 |
| `GltfEncoder` | sealed class | 支持 JSON 和 GLB 两种输出，GLB 使用 `GlbHeader.WriteTo` 编码文件头 |
| `GltfScanner` | ref struct | 零分配扫描，支持 `IsGlbFormat()`、`ScanGlbHeader()`、`ScanStatistics()`、`ScanExternalResources()` |
| `GltfStatistics` | sealed class | 统计信息（Version, SceneCount, NodeCount, MeshCount, TotalBufferSize 等） |

#### Acorn.Live2D — Live2D Cubism 模型格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]` `[扫描器接口]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `Live2DConstants` | static class | 常量（Moc3MagicNumber "MOC3"） |
| `Live2DModelData` | sealed class | 完整 moc3 模型（Version, IsBigEndian, Canvas, Parameters, Parts, Drawables, Deformers 等） |
| `Live2DCanvasInfo` / `Live2DParameter` / `Live2DPart` / `Live2DDrawable` / `Live2DDeformer` | sealed class | moc3 子结构 |
| `Live2DDecoder` | ref struct | 手动解码，支持版本 3/4/5，自动检测大小端 |
| `Live2DEncoder` | sealed class | 手动编码，写入 MOC3 头 → 偏移表 → 数据段 |
| `ILive2DScanner` | interface | 扫描器接口（ScanMoc3Header, ScanMoc3ParameterCount, ScanMoc3PartCount, ScanMoc3DrawableCount, ScanFileReferences） |
| `Live2DScanner` | ref struct : ILive2DScanner | 零分配扫描，支持 moc3 二进制和 model3.json |

#### Acorn.Spine — Spine 2D 骨骼动画格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]` `[扫描器接口]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `SpineProjectData` | sealed class | Spine 项目（SkeletonVersion, Hash, Width, Height, Bones, Slots, Skins, Animations 等） |
| `SpineAtlasData` / `SpineAtlasPage` / `SpineAtlasRegion` | sealed class | Atlas 纹理图集 |
| `SpineBone` / `SpineSlot` / `SpineSkin` / `SpineEvent` | sealed class | 骨骼/插槽/皮肤/事件 |
| `SpineIkConstraint` / `SpineTransformConstraint` / `SpinePathConstraint` | sealed class | 约束器 |
| `SpineAnimation` / `SpineTimeline` / `SpineKeyframe` | sealed class | 动画/时间线/关键帧 |
| `SpineDecoder` | ref struct | 手动解码，提供底层原语（ReadHeader, ReadString, ReadFloat, ReadLeb128Int32 等） |
| `SpineAtlasDecoder` | sealed class | 纯文本格式解析 Atlas |
| `SpineEncoder` | ref struct | 手动编码，提供底层原语（WriteHeader, WriteString, WriteFloat 等） |
| `SpineAtlasEncoder` | sealed class | 纯文本格式编码 Atlas |
| `ISpineScanner` | interface | 扫描器接口（ReadHash, ReadVersion, ReadSpineString 等） |
| `SpineScanner` | ref struct : ISpineScanner | 零分配扫描 |

***

### 9.6 图像/机器学习格式

#### Acorn.Psd — Adobe Photoshop 文档格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `PsdConstants` | static class | 常量（MagicNumber "8BPS", Version, BlendModeSignature），枚举（`PsdColorMode`, `PsdCompression`） |
| `PsdImageData` | sealed class | 完整 PSD 文件（Width, Height, Channels, Depth, ColorMode, Layers, MergedImageData） |
| `PsdLayer` | sealed class | 图层（Name, Bounds, ChannelCount, BlendMode, Opacity, IsVisible） |
| `PsdDecoder` | ref struct | 手动解码，大端序读取文件头 → 颜色模式 → 图像资源 → 图层/蒙版 → 图像数据 |
| `PsdEncoder` | ref struct | 手动编码，大端序写入 |
| `PsdScanner` | ref struct | 零分配扫描（ScanHeader, ScanLayerNames, ScanLayerCount） |

#### Acorn.Onnx — ONNX 机器学习模型格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `OnnxDataType` | enum | 数据类型（Undefined 到 Float8E5M2FNUZ） |
| `OnnxAttributeType` | enum | 属性类型（Undefined 到 Graphs） |
| `OnnxModelData` | sealed class | 完整 ONNX 模型（IrVersion, ProducerName, Graph, OpsetImport 等） |
| `OnnxGraph` / `OnnxNode` / `OnnxValueInfo` / `OnnxTensor` / `OnnxAttribute` | sealed class | ONNX 子结构 |
| `OnnxDecoder` | sealed class | 手动解码 Protobuf wire format，使用 LEB128 读取 Tag(fieldNumber + wireType) |
| `OnnxEncoder` | static class | 手动编码 Protobuf wire format |
| `OnnxScanner` | ref struct | 零分配扫描（ScanStatistics → IrVersion, NodeCount, InputCount 等） |

#### Acorn.SafeTensors — HuggingFace SafeTensors 格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `SafeTensorDType` | enum | 数据类型（Bool, UInt8, Int8, Int16, Int32, Int64, Float16, Float32, Float64, BFloat16） |
| `SafeTensorMeta` | sealed class | 张量元数据（DType, Shape, DataOffset, DataLength） |
| `SafeTensorsFileData` | sealed class | 完整文件（Tensors 字典, Metadata, Data） |
| `SafeTensorData` | sealed class | 单个张量（Name, DType, Shape, Data） |
| `SafeTensorsDecoder` | sealed class | 手动解码，8 字节头长度 → JSON 头 → 二进制数据 |
| `SafeTensorsEncoder` | sealed class | 手动编码，JSON 头（8 字节对齐） + 二进制数据 |
| `SafeTensorsScanner` | ref struct | 零分配扫描（ScanStatistics → HeaderSize, TensorCount, TotalParameters 等） |

***

### 9.7 数据库协议

#### Acorn.MySql — MySQL 网络协议

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]` `[手动编码]` `[帧协议]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `MySqlConstants` | static class | 常量（ProtocolVersion=10），枚举（`ServerStatus` `[Flags]`, `CommandType`, `ErrorCode`） |
| `MySqlPacketHeader` | `[BinarySerializable]` partial struct | 包头（Length0/1/2, SequenceId），含 `PayloadLength` 计算属性和 `Create` 工厂方法 |
| `MySqlPacketData` | class | 数据包（Length, SequenceId, Type, Data, CommandType, ErrorCode, ErrorMessage, ServerStatus） |
| `MySqlPacketType` | enum | 包类型（Handshake, HandshakeResponse, Command, Result, Error, Field, RowData, Eof） |
| `MySqlDecoder` | ref struct | 手动解码，使用 `MySqlPacketHeader.TryRead` 读取包头，支持长度编码整数 |
| `MySqlEncoder` | ref struct | 手动编码，使用 `MySqlPacketHeader.WriteTo` 写入包头，支持握手响应和查询命令 |
| `MySqlProtocol` | struct : `IFrameProtocol` | 帧协议（4 字节头：3 字节载荷长度 + 1 字节序号） |
| `MySqlScanner` | ref struct | 基于 `FrameScanner<MySqlProtocol>` 的帧扫描，含 `ScanFrameStatistics()` |
| `MySqlFrameStatistics` | sealed class | 帧统计（TotalFrames, TotalPayloadBytes, MaxPayloadSize, MinPayloadSize） |

#### Acorn.PostgreSql — PostgreSQL 网络协议

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]` `[帧协议]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `PostgreSqlConstants` | static class | 常量（ProtocolVersion=196608），枚举（`MessageType`, `AuthenticationType`, `TransactionStatus`） |
| `PostgreSqlMessageData` | class | 消息（Type, Length, Data, AuthenticationType, ErrorFields, CommandTag, TransactionStatus, FieldDescriptions） |
| `PostgreSqlFieldDescription` | class | 字段描述（Name, TableId, ColumnId, DataTypeOid, DataTypeSize, TypeModifier, FormatCode） |
| `PostgreSqlDecoder` | ref struct | 手动解码，大端序读取消息类型 + 长度 + 内容 |
| `PostgreSqlEncoder` | ref struct | 手动编码，支持 StartupMessage, QueryMessage, PasswordMessage, SyncMessage, TerminateMessage |
| `PostgreSqlProtocol` | struct : `IFrameProtocol` | 帧协议（1 字节消息类型 + 4 字节大端序长度 + 载荷） |
| `PostgreSqlScanner` | ref struct | 基于 `FrameScanner<PostgreSqlProtocol>` 的帧扫描 |
| `PostgreSqlFrameStatistics` | sealed class | 帧统计（TotalFrames, TotalPayloadBytes） |

***

### 9.8 消息协议

#### Acorn.Redis — RESP 协议

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `RedisConstants` | static class | 常量（SimpleStringPrefix '+', ErrorPrefix '-', IntegerPrefix ':', BulkStringPrefix '$', ArrayPrefix '*'），内嵌 `Commands` 类 |
| `RedisMessageType` | enum | 消息类型（SimpleString, Error, Integer, BulkString, Array） |
| `RedisMessageData` | class | 消息（Type, SimpleString, Error, Integer, BulkString, Array, IsNullBulkString, IsNullArray） |
| `RedisDecoder` | ref struct | 手动解码 RESP 文本协议，逐行解析（CRLF 分隔），递归解码数组和批量字符串 |
| `RedisEncoder` | ref struct | 手动编码，支持 SimpleString, Error, Integer, BulkString, Array, Command |
| `RedisScanner` | ref struct | 零分配扫描（ScanStatistics, ScanMessages） |

#### Acorn.ZeroMQ — ZMTP 协议

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]` `[帧协议]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `ZeroMQConstants` | static class | 常量（Version=3, FrameSize=8），内嵌枚举（`MessageType`, `CommandType`, `Flags` `[Flags]`, `SocketType`） |
| `ZeroMQFrameData` | class | 帧（Flags, Length, Data） |
| `ZeroMQMessageData` | class | 消息（Type, Length, Data, Flags, CommandType, SocketType, Address, Parts） |
| `ZeroMQDecoder` | ref struct | 手动解码，帧格式：1 字节 flags + 7 字节大端序长度 + 载荷 |
| `ZeroMQEncoder` | ref struct | 手动编码，支持单帧、多帧、命令消息 |
| `ZeroMQProtocol` | struct : `IFrameProtocol` | 帧协议（1 字节 flags + 8 字节小端序 uint64 长度 + 载荷） |
| `ZeroMQScanner` | ref struct | 基于 `FrameScanner<ZeroMQProtocol>` 的帧扫描 |
| `ZeroMQFrameStatistics` | sealed class | 帧统计（TotalFrames, TotalPayloadBytes, MultiPartMessages） |

#### Acorn.Protobuf — Protobuf Wire Format

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ✅

**实现模式**：`[手动解码]` `[手动编码]` `[帧协议]` `[自定义Codec]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `ProtobufConstants` | static class | 常量（MaxFieldNumber, FieldNumberMask, WireTypeMask），枚举（`WireType`: Varint, Fixed64, LengthDelimited, Fixed32） |
| `ProtobufMessageData` | class | 消息（Name, Fields, NestedMessages） |
| `ProtobufFieldData` | class | 字段（FieldNumber, Type, Name, IsRepeated, IsOptional, IsRequired） |
| `ProtobufStringCodec` | readonly struct : `ICodec<string>` | Protobuf 长度前缀字符串编解码器，基于 `Leb128UInt32` |
| `ProtobufDecoder` | ref struct | 手动解码 Protobuf wire format |
| `ProtobufEncoder` | ref struct | 手动编码 Protobuf wire format |
| `ProtobufFrameProtocol` | struct : `IFrameProtocol` | 帧协议（VarInt 长度前缀消息） |
| `ProtobufScanner` | ref struct | 零分配扫描（ScanFields, ScanStructure），逐字段解析 Tag(fieldNumber + wireType) |
| `ProtobufFieldInfo` | sealed record | 字段信息（FieldNumber, WireType, ValueType, VarintValue, Fixed32Value, Fixed64Value, LengthDelimitedValue） |
| `ProtobufValueType` | enum | 值类型（Varint, Fixed32, Fixed64, LengthDelimited） |

***

### 9.9 文档/着色器/Web/压缩格式

#### Acorn.Office — MS Office 二进制格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅（仅 Xls） `Scanner/` ✅（仅 Xls） `Codec/` ❌

**实现模式**：`[手动解码]` `[手动编码]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `OfficeConstants` | static class | 常量（Ole2MagicNumber），内嵌 `XlsRecordType` 类（Bof, Eof, Formula, Label, Number 等），内嵌 `PptRecordType` 类 |
| `ExcelWorkbookData` / `ExcelSheetData` / `ExcelRowData` / `ExcelCellData` | sealed class | Excel 数据结构 |
| `WordDocumentData` | sealed class | Word 数据结构（Text, Paragraphs） |
| `PowerPointData` | sealed class | PowerPoint 数据结构（Slides, SlideCount） |
| `XlsDecoder` | ref struct | 手动解码 BIFF 记录流（BoundSheet, SST） |
| `PptDecoder` | ref struct | 手动解码 PPT 记录流（TextCharsAtom） |
| `DocDecoder` | ref struct | 手动解码 OLE2 复合文档，读取 CLX 偏移和 Piece Table |
| `OpenXmlDecoder` | sealed class | 委托 `ZipDecoder` 解压，提取 XML 内容 |
| `XlsEncoder` | ref struct | 手动编码 BIFF 记录（BOF, WriteAccess, CodePage, BoundSheet, EOF） |
| `XlsScanner` | ref struct | 零分配扫描（ValidateHeader, ScanStatistics → SheetCount, RowCount 等） |

#### Acorn.SpirV — Khronos SPIR-V 着色器格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]` `[手动编码]` `[扫描器接口]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `SpirvConstants` | static class | 常量（MagicNumber=0x07230203, Version10~15） |
| `SpirvFileHeader` | `[BinarySerializable]` partial struct | 文件头（MagicNumber, Version, GeneratorMagic, Bound, Schema） |
| `SpirvOpCode` | enum : ushort | 约 80+ 操作码（OpNop=0 到 OpTypeAvcSicResultINTEL=5623） |
| `SpirvCapability` / `SpirvExecutionModel` / `SpirvExecutionMode` / `SpirvStorageClass` / `SpirvDecoration` / `SpirvBuiltIn` | enum : uint | SPIR-V 规范枚举 |
| `SpirvModuleData` | sealed class | 完整模块（MagicNumber, Version, GeneratorMagic, Bound, Schema, Instructions） |
| `SpirvInstruction` / `SpirvEntryPoint` / `SpirvDecorationInfo` / `SpirvName` / `SpirvTypeInfo` | sealed class | 指令/入口点/装饰/名称/类型 |
| `SpirvDecoder` | sealed class | 手动解码，文件头使用 `SpirvFileHeader.TryRead`，其余手动解析 32 位字流 |
| `SpirvEncoder` | sealed class | 手动编码，含 `EncodeString`（null 终止 + 4 字节对齐） |
| `ISpirvScanner` | interface | 扫描器接口（ReadSpirvWord, ReadInstructionHeader, ReadSpirvString） |
| `SpirvScanner` | ref struct : ISpirvScanner | 零分配扫描（ScanHeader, ScanStatistics, ScanEntryPointNames） |

#### Acorn.Wasm — WebAssembly 二进制格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Encode/` ✅ `Scanner/` ✅ `Codec/` ❌

**实现模式**：`[声明式]` `[手动解码]` `[手动编码]` `[帧协议]` `[扫描器接口]`

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `WasmConstants` | static class | 常量（MagicNumber "\0asm", Version=1, FunctionTypeForm=0x60），枚举（`WasmSectionId`, `WasmValueType`, `WasmExternalKind`, `WasmInitOpCode`） |
| `WasmHeader` | `[BinarySerializable]` partial struct | 文件头（Magic `FixedBytes4`, Version） |
| `WasmModuleData` | sealed class | 完整模块（Version, Types, Imports, Functions, Tables, Memories, Globals, Exports, Codes, DataSegments, CustomSections） |
| `WasmFunctionType` / `WasmLimits` / `WasmImport` / `WasmExport` / `WasmCode` / ... | sealed class | Wasm 子结构 |
| `WasmDecoder` | static class | 文件头使用 `WasmHeader.TryRead`，其余手动 + LEB128 解码 11 种段 |
| `WasmEncoder` | static class | 手动编码，完整的 11 种段编码方法 |
| `WasmProtocol` | struct : `IFrameProtocol` | 帧协议（1 字节段 ID + LEB128 段大小 + 段内容） |
| `IWasmScanner` | interface | 扫描器接口（ReadVersion, ReadName, ReadLeb128UInt32） |
| `WasmScanner` | ref struct | 零分配扫描（ScanHeader, ScanStatistics, ScanExports） |

#### Acorn.Zip — ZIP 归档格式

**目录结构**：`Data/` ✅ `Decode/` ✅ `Scanner/` ✅ `Encode/` ❌ `Codec/` ❌

**实现模式**：`[手动解码]`（Scanner 部分）

| 核心类型 | 类型 | 说明 |
|:---|:---|:---|
| `ZipEntryData` | sealed class | 条目（Name, Size, CompressedSize, CompressionMethod, Data） |
| `ZipFileData` | sealed class | 完整 ZIP 文件（Entries, EntryCount） |
| `ZipDecoder` | sealed class | 委托 `System.IO.Compression.ZipArchive` 解码，非手动二进制解析 |
| `ZipScanner` | ref struct | 零分配扫描（ValidateHeader 验证 PK 魔数, ScanStatistics → EntryCount, TotalCompressedSize 等） |

***

### 9.10 附录

#### 实现模式汇总

| 项目 | 声明式 | 手动解码 | 手动编码 | 帧协议 | 扫描器接口 | 自定义 Codec |
|:---|:---|:---|:---|:---|:---|:---|
| COFF | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| ELF | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| MachO | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| PE | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| DWARF | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| LLVM | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Gltf | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Live2D | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Spine | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Psd | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Onnx | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| SafeTensors | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| MySql | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| PostgreSql | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Redis | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| ZeroMQ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Protobuf | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Office | ❌ | ✅ | ✅（仅 Xls） | ❌ | ❌ | ❌ |
| SpirV | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Wasm | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Zip | ❌ | ✅（Scanner） | ❌ | ❌ | ❌ | ❌ |

#### 功能矩阵

| 项目 | Decode | Encode | Scanner | IFrameProtocol | ICodec | IXxxScanner |
|:---|:---|:---|:---|:---|:---|:---|
| COFF | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| ELF | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| MachO | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| PE | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| DWARF | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| LLVM | ✅ | ❌ | ✅（static） | ❌ | ❌ | ❌ |
| Gltf | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ❌ |
| Live2D | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ✅ |
| Spine | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ✅ |
| Psd | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ❌ |
| Onnx | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ❌ |
| SafeTensors | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ❌ |
| MySql | ✅ | ✅ | ✅（ref struct） | ✅ | ❌ | ❌ |
| PostgreSql | ✅ | ✅ | ✅（ref struct） | ✅ | ❌ | ❌ |
| Redis | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ❌ |
| ZeroMQ | ✅ | ✅ | ✅（ref struct） | ✅ | ❌ | ❌ |
| Protobuf | ✅ | ✅ | ✅（ref struct） | ✅ | ✅ | ❌ |
| Office | ✅ | ✅（仅 Xls） | ✅（仅 Xls） | ❌ | ❌ | ❌ |
| SpirV | ✅ | ✅ | ✅（ref struct） | ❌ | ❌ | ✅ |
| Wasm | ✅ | ✅ | ✅（ref struct） | ✅ | ❌ | ✅ |
| Zip | ✅ | ❌ | ✅（ref struct） | ❌ | ❌ | ❌ |

***

## 📚 参考文档

- [架构设计](../documentation/architecture.md) - 核心接口详解
- [特性系统](../documentation/attributes.md) - 声明式特性
- [扩展点](../documentation/extensibility.md) - 自定义扩展
- [典型用例](../documentation/examples.md) - 现有格式实现参考

***

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
