# Acorn.MachO

Acorn Mach-O 格式库，提供 macOS/iOS 可执行文件（.macho, .dylib）的扫描和解码功能。

## 功能特性

- **Mach-O 文件解码**：解析 macOS/iOS 可执行文件格式
- **Mach-O 文件扫描**：快速扫描 Mach-O 文件结构
- **支持多种架构**：x86, x64, ARM, ARM64, PowerPC
- **支持多种字节序**：小端和大端
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口

## 安装

```bash
dotnet add package Acorn.MachO
```

## 使用示例

### 解码 Mach-O 文件

```csharp
using Acorn.MachO.Decode;

var data = File.ReadAllBytes("example.macho");

var decoder = new MachODecoder();
var machoFile = decoder.Decode(data);

Console.WriteLine($"File type: {(machoFile.Header.IsExecutable ? "Executable" : "Dynamic Library")}");
Console.WriteLine($"Architecture: {(machoFile.Header.Is64Bit ? "x64" : "x86")}");
Console.WriteLine($"Sections: {machoFile.Sections.Count}");

foreach (var section in machoFile.Sections)
{
    Console.WriteLine($"- {section.SectionName}: {section.Size} bytes");
}
```

### 扫描 Mach-O 文件

```csharp
using Acorn.MachO.Scanner;

var data = File.ReadAllBytes("example.macho");

var scanResult = MachOScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.MachO/`
  - `Data/` - 数据结构
    - `MachOFileData.cs` - Mach-O 文件数据结构
  - `Decode/` - 解码器
    - `MachODecoder.cs` - Mach-O 文件解码器
  - `Scanner/` - 扫描器
    - `MachOScanner.cs` - Mach-O 文件扫描器

## 支持的格式

- **Mach-O 32**：32 位 Mach-O 文件
- **Mach-O 64**：64 位 Mach-O 文件
- **Executable**：可执行文件
- **Dynamic Library**：动态库 (.dylib)

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
