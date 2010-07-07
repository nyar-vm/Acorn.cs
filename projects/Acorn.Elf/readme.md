# Acorn.ELF

Acorn ELF 格式库，提供 Linux 可执行文件（.elf, .so）的扫描和解码功能。

## 功能特性

- **ELF 文件解码**：解析 Linux 可执行文件格式
- **ELF 文件扫描**：快速扫描 ELF 文件结构
- **支持多种架构**：x86, x64, ARM, ARM64, MIPS, PowerPC, RISC-V
- **支持多种字节序**：小端和大端
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口

## 安装

```bash
dotnet add package Acorn.ELF
```

## 使用示例

### 解码 ELF 文件

```csharp
using Acorn.ELF.Decode;

var data = File.ReadAllBytes("example.elf");

var decoder = new ELFDecoder();
var elfFile = decoder.Decode(data);

Console.WriteLine($"File type: {(elfFile.IsExecutable ? "Executable" : "Shared Library")}");
Console.WriteLine($"Architecture: {(elfFile.Header.Is64Bit ? "x64" : "x86")}");
Console.WriteLine($"Sections: {elfFile.Header.SectionHeaderCount}");
Console.WriteLine($"Entry point: 0x{elfFile.Header.EntryPoint:X16}");

foreach (var section in elfFile.SectionHeaders)
{
    Console.WriteLine($"- {section.Name}: {section.Size} bytes");
}
```

### 扫描 ELF 文件

```csharp
using Acorn.ELF.Scanner;

var data = File.ReadAllBytes("example.elf");

var scanResult = ELFScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.ELF/`
  - `Data/` - 数据结构
    - `ELFFileData.cs` - ELF 文件数据结构
  - `Decode/` - 解码器
    - `ELFDecoder.cs` - ELF 文件解码器
  - `Scanner/` - 扫描器
    - `ELFScanner.cs` - ELF 文件扫描器

## 支持的格式

- **ELF32**：32 位 ELF 文件
- **ELF64**：64 位 ELF 文件
- **Executable**：可执行文件
- **Shared Library**：共享库 (.so)

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
