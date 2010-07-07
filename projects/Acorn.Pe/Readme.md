# Acorn.Pe

Acorn PE 格式库，提供 Windows 可执行文件（.exe, .dll）的扫描和解码功能。

## 功能特性

- **PE 文件解码**：解析 Windows 可执行文件格式
- **PE 文件扫描**：快速扫描 PE 文件结构
- **支持多种架构**：x86, x64, ARM, ARM64, IA64
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口
- **与 Acorn.Coff 集成**：复用 COFF 目标文件解析能力

## 安装

```bash
dotnet add package Acorn.Pe
```

## 使用示例

### 解码 PE 文件

```csharp
using Acorn.Pe.Decode;

var data = File.ReadAllBytes("example.exe");

var decoder = new PeDecoder();
var peFile = decoder.Decode(data);

Console.WriteLine($"File type: {(peFile.IsDll ? "DLL" : "EXE")}");
Console.WriteLine($"Architecture: {(peFile.Is64Bit ? "x64" : "x86")}");
Console.WriteLine($"Sections: {peFile.Header.NumberOfSections}");
Console.WriteLine($"Entry point: 0x{peFile.OptionalHeader.AddressOfEntryPoint:X8}");

foreach (var section in peFile.Sections)
{
    Console.WriteLine($"- {section.Name}: {section.SizeOfRawData} bytes");
}
```

### 扫描 PE 文件

```csharp
using Acorn.Pe.Scanner;

var data = File.ReadAllBytes("example.exe");

var scanResult = PeScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.Pe/`
  - `Data/` - 数据结构
    - `PeFileData.cs` - PE 文件数据结构
  - `Decode/` - 解码器
    - `PeDecoder.cs` - PE 文件解码器
  - `Scanner/` - 扫描器
    - `PeScanner.cs` - PE 文件扫描器

## 支持的格式

- **PE32**：32 位 Windows 可执行文件
- **PE32+**：64 位 Windows 可执行文件
- **DLL**：动态链接库
- **EXE**：可执行文件

## 依赖

- .NET 11.0+
- Acorn.Core
- Acorn.Coff

## 许可证

MPL-2.0
