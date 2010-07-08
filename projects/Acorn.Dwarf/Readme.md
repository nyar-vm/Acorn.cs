# Acorn.DWARF

Acorn DWARF 格式库，提供调试信息（.debug）的扫描和解码功能。

## 功能特性

- **DWARF 文件解码**：解析调试信息格式
- **DWARF 文件扫描**：快速扫描 DWARF 文件结构
- **支持 DWARF 2/3/4/5**：兼容多个 DWARF 版本
- **编译单元解析**：提取编译单元信息
- **行号表解析**：提取源码行号映射
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口

## 安装

```bash
dotnet add package Acorn.DWARF
```

## 使用示例

### 解码 DWARF 文件

```csharp
using Acorn.DWARF.Decode;

var data = File.ReadAllBytes("example.debug");

var decoder = new DWARFDecoder();
var dwarfFile = decoder.Decode(data);

Console.WriteLine($"Compilation units: {dwarfFile.CompilationUnits.Count}");
foreach (var unit in dwarfFile.CompilationUnits)
{
    Console.WriteLine($"- DWARF {unit.Version}: {unit.Entries.Count} entries");
}
```

### 扫描 DWARF 文件

```csharp
using Acorn.DWARF.Scanner;

var data = File.ReadAllBytes("example.debug");

var scanResult = DWARFScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.DWARF/`
  - `Data/` - 数据结构
    - `DWARFData.cs` - DWARF 文件数据结构
  - `Decode/` - 解码器
    - `DWARFDecoder.cs` - DWARF 文件解码器
  - `Scanner/` - 扫描器
    - `DWARFScanner.cs` - DWARF 文件扫描器

## 支持的格式

- **DWARF 2**：调试信息格式版本 2
- **DWARF 3**：调试信息格式版本 3
- **DWARF 4**：调试信息格式版本 4
- **DWARF 5**：调试信息格式版本 5

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
