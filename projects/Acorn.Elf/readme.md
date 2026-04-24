# 📦 Acorn.ELF

ELF（Executable and Linkable Format）可执行文件格式编解码器。

## 📐 格式布局

ELF 是 Linux/Unix 系统的标准可执行文件格式，支持可执行文件、目标文件、共享库等。

### ELF 头（ELF Header）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Magic | 0x00 | 4 | 魔数 `0x7F 'E' 'L' 'F'` | `ELFHeaderData.Magic` |
| Class | 0x04 | 1 | 类别（1=32位, 2=64位） | `ELFHeaderData.Class` |
| DataEncoding | 0x05 | 1 | 数据编码（1=小端, 2=大端） | `ELFHeaderData.DataEncoding` |
| Version | 0x06 | 1 | ELF 版本（通常为1） | `ELFHeaderData.Version` |
| OSABI | 0x07 | 1 | OS/ABI 标识 | `ELFHeaderData.OSABI` |
| ABIVersion | 0x08 | 1 | ABI 版本 | `ELFHeaderData.ABIVersion` |
| Padding | 0x09 | 7 | 填充 | - |
| Type | 0x10 | 2 | 文件类型（1=重定位, 2=可执行, 3=共享库） | `ELFHeaderData.Type` |
| Machine | 0x12 | 2 | 目标机器类型 | `ELFHeaderData.Machine` |
| ObjectVersion | 0x14 | 4 | 对象文件版本 | `ELFHeaderData.ObjectVersion` |
| EntryPoint | 0x18 | 4/8 | 入口点虚拟地址 | `ELFHeaderData.EntryPoint` |
| ProgramHeaderOffset | 0x1C/0x20 | 4/8 | 程序头表文件偏移 | `ELFHeaderData.ProgramHeaderOffset` |
| SectionHeaderOffset | 0x20/0x28 | 4/8 | 节区头表文件偏移 | `ELFHeaderData.SectionHeaderOffset` |
| Flags | 0x24/0x30 | 4 | 处理器特定标志 | `ELFHeaderData.Flags` |
| ELFHeaderSize | 0x28/0x34 | 2 | ELF 头大小 | `ELFHeaderData.ELFHeaderSize` |
| ProgramHeaderSize | 0x2A/0x36 | 2 | 程序头项大小 | `ELFHeaderData.ProgramHeaderSize` |
| ProgramHeaderCount | 0x2C/0x38 | 2 | 程序头项数量 | `ELFHeaderData.ProgramHeaderCount` |
| SectionHeaderSize | 0x2E/0x3A | 2 | 节区头项大小 | `ELFHeaderData.SectionHeaderSize` |
| SectionHeaderCount | 0x30/0x3C | 2 | 节区头项数量 | `ELFHeaderData.SectionHeaderCount` |
| StringTableIndex | 0x32/0x3E | 2 | 节区名称字符串表索引 | `ELFHeaderData.StringTableIndex` |

### 程序头（Program Header）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| Type | 4 | 段类型（LOAD/DYNAMIC/INTERP 等） | `ELFProgramHeaderData.Type` |
| Flags | 4 (64位) | 段标志（R/W/X） | `ELFProgramHeaderData.Flags` |
| Offset | 4/8 | 段文件偏移 | `ELFProgramHeaderData.Offset` |
| VirtualAddress | 4/8 | 段虚拟地址 | `ELFProgramHeaderData.VirtualAddress` |
| PhysicalAddress | 4/8 | 段物理地址 | `ELFProgramHeaderData.PhysicalAddress` |
| FileSize | 4/8 | 段文件大小 | `ELFProgramHeaderData.FileSize` |
| MemorySize | 4/8 | 段内存大小 | `ELFProgramHeaderData.MemorySize` |
| Alignment | 4/8 | 段对齐 | `ELFProgramHeaderData.Alignment` |

### 节区头（Section Header）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| Name | 4 | 节区名称字符串表偏移 | `ELFSectionHeaderData.Name` |
| Type | 4 | 节区类型 | `ELFSectionHeaderData.Type` |
| Flags | 4/8 | 节区标志 | `ELFSectionHeaderData.Flags` |
| Address | 4/8 | 节区虚拟地址 | `ELFSectionHeaderData.Address` |
| Offset | 4/8 | 节区文件偏移 | `ELFSectionHeaderData.Offset` |
| Size | 4/8 | 节区大小 | `ELFSectionHeaderData.Size` |
| Link | 4 | 关联节区索引 | `ELFSectionHeaderData.Link` |
| Info | 4 | 额外信息 | `ELFSectionHeaderData.Info` |
| Alignment | 4/8 | 节区对齐 | `ELFSectionHeaderData.Alignment` |
| EntrySize | 4/8 | 固定大小条目的大小 | `ELFSectionHeaderData.EntrySize` |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `ELFHeaderData` | ELF 文件头 | [Data/ELFFileData.cs](Data/ELFFileData.cs) |
| `ELFProgramHeaderData` | 程序头 | [Data/ELFFileData.cs](Data/ELFFileData.cs) |
| `ELFSectionHeaderData` | 节区头 | [Data/ELFFileData.cs](Data/ELFFileData.cs) |
| `ELFFileData` | ELF 文件完整数据 | [Data/ELFFileData.cs](Data/ELFFileData.cs) |
| `ELFDecoder` | ELF 解码器 | [Decode/ELFDecoder.cs](Decode/ELFDecoder.cs) |
| `ELFScanner` | ELF 扫描器 | [Scanner/ELFScanner.cs](Scanner/ELFScanner.cs) |

## 📚 格式规范参考

- [System V ABI - ELF 规范](https://refspecs.linuxfoundation.org/elf/elf.pdf)
- [ELF Format Reference](https://refspecs.linuxbase.org/elf/gabi4+/ch4.eheader.html)
- [OSDev ELF Wiki](https://wiki.osdev.org/ELF)
