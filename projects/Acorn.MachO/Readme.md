# 📦 Acorn.MachO

Mach-O（Mach Object）macOS/iOS 可执行文件格式编解码器。

## 📐 格式布局

### Mach-O 文件头（Mach Header）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Magic | 0x00 | 4 | `0xFEEDFACE`(32位) / `0xFEEDFACF`(64位) | `MachOHeaderData.Magic` |
| CPUType | 0x04 | 4 | CPU 类型（如 X86=7, ARM64=0x0100000C） | `MachOHeaderData.CPUType` |
| CPUSubtype | 0x08 | 4 | CPU 子类型 | `MachOHeaderData.CPUSubtype` |
| FileType | 0x0C | 4 | 文件类型（1=对象, 2=可执行, 6=动态库） | `MachOHeaderData.FileType` |
| NumberOfLoadCommands | 0x10 | 4 | 加载命令数量 | `MachOHeaderData.NumberOfLoadCommands` |
| SizeOfLoadCommands | 0x14 | 4 | 加载命令总大小 | `MachOHeaderData.SizeOfLoadCommands` |
| Flags | 0x18 | 4 | 标志 | `MachOHeaderData.Flags` |
| Reserved | 0x1C | 4 | 保留（64位） | `MachOHeaderData.Reserved` |

### 加载命令（Load Command）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| Command | 4 | 命令类型 | `MachOLoadCommandData.Command` |
| Size | 4 | 命令大小 | `MachOLoadCommandData.Size` |
| Data | 变长 | 命令数据 | `MachOLoadCommandData.Data` |

### 常见加载命令类型

| 值 | 名称 | 说明 |
|---|---|---|
| 0x01 | LC_SEGMENT | 32位段加载 |
| 0x19 | LC_SEGMENT_64 | 64位段加载 |
| 0x02 | LC_SYMTAB | 符号表 |
| 0x0B | LC_DYSYMTAB | 动态符号表 |
| 0x0C | LC_LOAD_DYLIB | 加载动态库 |
| 0x22 | LC_LOAD_DYLINKER | 加载动态链接器 |
| 0x28 | LC_MAIN | 主程序入口 |

### 段（Segment）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| SegName | 16 | 段名称 | `MachOSegmentData.SegName` |
| VMAddr | 4/8 | 虚拟地址 | `MachOSegmentData.VMAddr` |
| VMSize | 4/8 | 虚拟大小 | `MachOSegmentData.VMSize` |
| FileOffset | 4/8 | 文件偏移 | `MachOSegmentData.FileOffset` |
| FileSize | 4/8 | 文件大小 | `MachOSegmentData.FileSize` |
| MaxProt | 4 | 最大保护 | `MachOSegmentData.MaxProt` |
| InitProt | 4 | 初始保护 | `MachOSegmentData.InitProt` |
| NumberOfSections | 4 | 节区数量 | `MachOSegmentData.NumberOfSections` |
| Flags | 4 | 标志 | `MachOSegmentData.Flags` |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `MachOHeaderData` | Mach-O 文件头 | [Data/MachOFileData.cs](Data/MachOFileData.cs) |
| `MachOLoadCommandData` | 加载命令 | [Data/MachOFileData.cs](Data/MachOFileData.cs) |
| `MachOSegmentData` | 段数据 | [Data/MachOFileData.cs](Data/MachOFileData.cs) |
| `MachOSectionData` | 节区数据 | [Data/MachOFileData.cs](Data/MachOFileData.cs) |
| `MachOFileData` | Mach-O 文件完整数据 | [Data/MachOFileData.cs](Data/MachOFileData.cs) |
| `MachODecoder` | Mach-O 解码器 | [Decode/MachODecoder.cs](Decode/MachODecoder.cs) |
| `MachOScanner` | Mach-O 扫描器 | [Scanner/MachOScanner.cs](Scanner/MachOScanner.cs) |

## 📚 格式规范参考

- [OS X ABI Mach-O File Format Reference](https://developer.apple.com/library/archive/documentation/Performance/Conceptual/CodeFootprint/Articles/MachOOverview.html)
- [Mach-O File Format Reference](https://developer.apple.com/library/archive/documentation/DeveloperTools/Conceptual/MachOTopics/0-Introduction/introduction.html)
- [OSDev Mach-O Wiki](https://wiki.osdev.org/Mach-O)
