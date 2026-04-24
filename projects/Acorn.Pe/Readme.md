# 📦 Acorn.PE

PE（Portable Executable）Windows 可执行文件格式编解码器。

## 📐 格式布局

### DOS 头（DOS Header）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| e_magic | 0x00 | 2 | DOS 魔数 `"MZ"`（0x5A4D） | `PeHeaderData.DosMagic` |
| ... | 0x02 | 58 | DOS 存根和填充 | - |
| e_lfanew | 0x3C | 4 | PE 头偏移 | `PeHeaderData.PeHeaderOffset` |

### PE 签名（PE Signature）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| PE Signature | 0x00 | 4 | `"PE\0\0"`（0x50450000） | `PeHeaderData.PeMagic` |

### COFF 文件头（File Header）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Machine | 0x00 | 2 | 机器类型 | `PeHeaderData.Machine` |
| NumberOfSections | 0x02 | 2 | 节区数量 | `PeHeaderData.NumberOfSections` |
| TimeDateStamp | 0x04 | 4 | 时间戳 | `PeHeaderData.TimeDateStamp` |
| PointerToSymbolTable | 0x08 | 4 | 符号表偏移 | `PeHeaderData.PointerToSymbolTable` |
| NumberOfSymbols | 0x0C | 4 | 符号数量 | `PeHeaderData.NumberOfSymbols` |
| SizeOfOptionalHeader | 0x10 | 2 | 可选头大小 | `PeHeaderData.SizeOfOptionalHeader` |
| Characteristics | 0x12 | 2 | 特征标志 | `PeHeaderData.Characteristics` |

### 可选头（Optional Header）

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Magic | 0x00 | 2 | 0x10B=PE32, 0x20B=PE32+ | `PeOptionalHeaderData.Magic` |
| MajorLinkerVersion | 0x02 | 1 | 链接器主版本 | `PeOptionalHeaderData.MajorLinkerVersion` |
| MinorLinkerVersion | 0x03 | 1 | 链接器次版本 | `PeOptionalHeaderData.MinorLinkerVersion` |
| SizeOfCode | 0x04 | 4 | 代码节大小 | `PeOptionalHeaderData.SizeOfCode` |
| SizeOfInitializedData | 0x08 | 4 | 已初始化数据大小 | `PeOptionalHeaderData.SizeOfInitializedData` |
| SizeOfUninitializedData | 0x0C | 4 | 未初始化数据大小 | `PeOptionalHeaderData.SizeOfUninitializedData` |
| AddressOfEntryPoint | 0x10 | 4 | 入口点 RVA | `PeOptionalHeaderData.AddressOfEntryPoint` |
| BaseOfCode | 0x14 | 4 | 代码基址 RVA | `PeOptionalHeaderData.BaseOfCode` |
| ImageBase | 0x18/0x1C | 4/8 | 镜像基址 | `PeOptionalHeaderData.ImageBase` |
| SectionAlignment | 0x1C/0x20 | 4 | 节区对齐 | `PeOptionalHeaderData.SectionAlignment` |
| FileAlignment | 0x20/0x24 | 4 | 文件对齐 | `PeOptionalHeaderData.FileAlignment` |
| MajorOSVersion | 0x24/0x28 | 2 | 所需 OS 主版本 | `PeOptionalHeaderData.MajorOSVersion` |
| SizeOfImage | 0x38/0x40 | 4 | 镜像大小 | `PeOptionalHeaderData.SizeOfImage` |
| SizeOfHeaders | 0x3C/0x44 | 4 | 头大小 | `PeOptionalHeaderData.SizeOfHeaders` |
| DataDirectory | 0x60/0x70 | 128/144 | 数据目录表 | `PeOptionalHeaderData.DataDirectory` |

### 数据目录（Data Directory）

| 索引 | 名称 | 说明 |
|---|---|---|
| 0 | EXPORT | 导出表 |
| 1 | IMPORT | 导入表 |
| 2 | RESOURCE | 资源表 |
| 3 | EXCEPTION | 异常表 |
| 4 | CERTIFICATE | 证书表 |
| 5 | BASE_RELOCATION | 基址重定位表 |
| 6 | DEBUG | 调试信息 |
| 7 | ARCHITECTURE | 架构特定数据 |
| 8 | GLOBAL_PTR | 全局指针 |
| 9 | TLS | 线程本地存储 |
| 10 | LOAD_CONFIG | 加载配置 |
| 11 | BOUND_IMPORT | 绑定导入 |
| 12 | IAT | 导入地址表 |
| 13 | DELAY_IMPORT | 延迟导入描述符 |
| 14 | COM_DESCRIPTOR | CLR 运行时头 |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `PeHeaderData` | PE 文件头 | [Data/PEFileData.cs](Data/PEFileData.cs) |
| `PeOptionalHeaderData` | PE 可选头 | [Data/PEFileData.cs](Data/PEFileData.cs) |
| `PeSectionHeaderData` | 节区头 | [Data/PEFileData.cs](Data/PEFileData.cs) |
| `PeDataDirectory` | 数据目录项 | [Data/PEFileData.cs](Data/PEFileData.cs) |
| `PeFileData` | PE 文件完整数据 | [Data/PEFileData.cs](Data/PEFileData.cs) |
| `PEDecoder` | PE 解码器 | [Decode/PEDecoder.cs](Decode/PEDecoder.cs) |
| `PEScanner` | PE 扫描器 | [Scanner/PEScanner.cs](Scanner/PEScanner.cs) |

## 📚 格式规范参考

- [Microsoft PE/COFF 规范](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)
- [PE Format Deep Dive](https://0xrick.github.io/win-internals/pe1/)
