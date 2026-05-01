using System.Text;
using Acorn.ELF.Data;
using Acorn.ELF.Decode;

namespace Acorn.ELF.Scanner;

/// <summary>
///     ELF 文件扫描器，提供对 Linux 可执行文件的快速结构扫描。
/// </summary>
public class ELFScanner
{
    /// <summary>
    ///     扫描 ELF 文件，提取结构信息。
    /// </summary>
    /// <param name="data">ELF 二进制数据。</param>
    /// <returns>扫描结果。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("ELF File Scan Result:");
        result.AppendLine("===================");

        if (data.Length < 4)
        {
            result.AppendLine("❌ 文件数据过短");
            return result.ToString();
        }

        if (data[0] != ElfConstants.Magic[0] || data[1] != ElfConstants.Magic[1] || data[2] != ElfConstants.Magic[2] || data[3] != ElfConstants.Magic[3])
        {
            result.AppendLine("❌ 不是有效的 ELF 文件");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 ELF 文件");
        result.AppendLine();

        try
        {
            var decoder = new ELFDecoder();
            var elfFile = decoder.Decode(data);

            var fileType = elfFile.IsExecutable ? "🚀 可执行文件" :
                          elfFile.IsSharedLibrary ? "📦 共享库 (SO)" : "📄 其他";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = elfFile.Header.Is64Bit ? "x64 (64位)" : "x86 (32位)";
            result.AppendLine($"架构: {architecture}");

            var endianness = elfFile.Header.IsLittleEndian ? "小端 (Little Endian)" : "大端 (Big Endian)";
            result.AppendLine($"字节序: {endianness}");

            var machineType = GetMachineTypeName(elfFile.Header.Machine);
            result.AppendLine($"机器类型: {machineType}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {elfFile.Header.SectionHeaderCount}");
            if (elfFile.SectionHeaders.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in elfFile.SectionHeaders)
                {
                    result.AppendLine($"  📄 {section.Name}");
                    result.AppendLine($"     类型: {GetSectionTypeName(section.Type)}, 大小: {section.Size} bytes");
                    result.AppendLine($"     虚拟地址: 0x{section.Address:X16}, 文件偏移: 0x{section.Offset:X16}");
                }
            }

            result.AppendLine();

            result.AppendLine($"📦 段数量: {elfFile.Header.ProgramHeaderCount}");
            if (elfFile.ProgramHeaders.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("段列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var program in elfFile.ProgramHeaders)
                {
                    result.AppendLine($"  📄 {GetSegmentTypeName(program.Type)}");
                    result.AppendLine($"     虚拟地址: 0x{program.VirtualAddress:X16}, 文件大小: {program.FileSize} bytes");
                    result.AppendLine($"     内存大小: {program.MemorySize} bytes, 对齐: {program.Alignment}");
                }
            }

            result.AppendLine();
            result.AppendLine($"入口点: 0x{elfFile.Header.EntryPoint:X16}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误: {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     获取机器类型名称。
    /// </summary>
    private static string GetMachineTypeName(ushort machine)
    {
        return machine switch
        {
            0x00 => "未知",
            0x02 => "SPARC",
            0x03 => "x86",
            0x08 => "MIPS",
            0x14 => "PowerPC",
            0x28 => "ARM",
            0x2A => "SuperH",
            0x32 => "IA-64",
            0x3E => "x86-64",
            0xB7 => "AArch64",
            0xF3 => "RISC-V",
            _ => $"未知 (0x{machine:X4})"
        };
    }

    /// <summary>
    ///     获取节区类型名称。
    /// </summary>
    private static string GetSectionTypeName(uint type)
    {
        return type switch
        {
            0x00 => "NULL",
            0x01 => "PROGBITS",
            0x02 => "SYMTAB",
            0x03 => "STRTAB",
            0x04 => "RELA",
            0x05 => "HASH",
            0x06 => "DYNAMIC",
            0x07 => "NOTE",
            0x08 => "NOBITS",
            0x09 => "REL",
            0x0A => "SHLIB",
            0x0B => "DYNSYM",
            0x0E => "INIT_ARRAY",
            0x0F => "FINI_ARRAY",
            0x10 => "PREINIT_ARRAY",
            0x11 => "GROUP",
            0x12 => "SYMTAB_SHNDX",
            0x13 => "NUM",
            _ => $"未知 (0x{type:X8})"
        };
    }

    /// <summary>
    ///     获取段类型名称。
    /// </summary>
    private static string GetSegmentTypeName(uint type)
    {
        return type switch
        {
            0x00 => "NULL",
            0x01 => "LOAD",
            0x02 => "DYNAMIC",
            0x03 => "INTERP",
            0x04 => "NOTE",
            0x05 => "SHLIB",
            0x06 => "PHDR",
            0x07 => "TLS",
            _ => $"未知 (0x{type:X8})"
        };
    }
}
