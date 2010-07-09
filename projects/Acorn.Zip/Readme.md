# Acorn.Zip

Acorn ZIP 格式库，提供 ZIP 压缩包的扫描和解码功能。

## 功能特性

- **ZIP 文件解码**：解析 ZIP 压缩包结构
- **ZIP 条目提取**：获取指定条目的数据
- **ZIP 文件扫描**：快速扫描 ZIP 文件结构
- **轻量级实现**：基于 .NET 的 ZipArchive，提供简化的 API
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口

## 安装

```bash
dotnet add package Acorn.Zip
```

## 使用示例

### 解码 ZIP 文件

```csharp
using Acorn.Zip.Decode;

var data = File.ReadAllBytes("example.zip");

var decoder = new ZipDecoder();
var zipFile = decoder.Decode(data);

Console.WriteLine($"ZIP contains {zipFile.EntryCount} entries:");
foreach (var entry in zipFile.Entries)
{
    Console.WriteLine($"- {entry.Name}: {entry.Size} bytes");
}
```

### 获取指定条目

```csharp
using Acorn.Zip.Decode;

var data = File.ReadAllBytes("example.zip");

var decoder = new ZipDecoder();
var entryData = decoder.GetEntry(data, "document.xml");

if (entryData != null)
{
    Console.WriteLine($"Entry size: {entryData.Length} bytes");
}
```

### 扫描 ZIP 文件

```csharp
using Acorn.Zip.Scanner;

var data = File.ReadAllBytes("example.zip");

var scanResult = ZipScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.Zip/`
  - `Data/` - 数据结构
    - `ZipFileData.cs` - ZIP 文件数据结构
  - `Decode/` - 解码器
    - `ZipDecoder.cs` - ZIP 文件解码器
  - `Scanner/` - 扫描器
    - `ZipScanner.cs` - ZIP 文件扫描器

## 支持的格式

- **ZIP**：标准 ZIP 压缩包格式

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
