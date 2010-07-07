using System.Text;
using Acorn.Coff.Data;
using Acorn.Coff.Decode;

namespace Acorn.Coff.Scanner;

/// <summary>
///     COFF 文件扫描器，提供对 Windows 目标文件的快速结构扫描。
/// </summary>
public class CoffScanner
{
    /// <summary>
    ///     扫描 COFF 文件，提取结构信息。
    /// </summary>
    /// <param name="data">COFF 二进制数据。</param>
    /// <returns>扫描结果。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("COFF File Scan Result:");
        result.AppendLine("=====================");

        if (data.Length < 20)
        {
            result.AppendLine("❌ 文件数据过短");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 COFF 文件");
        result.AppendLine();

        try
        {
            var decoder = new CoffDecoder();
            var coffFile = decoder.Decode(data);

            var machineType = GetMachineTypeName(coffFile.Header.Machine);
            result.AppendLine($"机器类型: {machineType}");

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(coffFile.Header.TimeDateStamp);
            result.AppendLine($"编译时间: {timestamp:yyyy-MM-dd HH:mm:ss}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {coffFile.Header.NumberOfSections}");
            if (coffFile.Sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in coffFile.Sections)
                {
                    result.AppendLine($"  📄 {section.Name}");
                    result.AppendLine($"     虚拟地址: 0x{section.VirtualAddress:X8}, 原始大小: {section.SizeOfRawData} bytes");
                    result.AppendLine($"     重定位: {section.NumberOfRelocations}, 行号: {section.NumberOfLinenumbers}");
                }
            }

            result.AppendLine();

            result.AppendLine($"🔣 符号数量: {coffFile.Symbols.Count}");
            if (coffFile.Symbols.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("符号列表（前 10 个）:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var symbol in coffFile.Symbols.Take(10))
                {
                    var section = symbol.SectionNumber switch
                    {
                        0 => "UNDEF",
                        -1 => "ABS",
                        -2 => "DEBUG",
                        _ => $"SEC{symbol.SectionNumber}"
                    };

                    result.AppendLine($"  {symbol.Name} ({section}) = 0x{symbol.Value:X8}");
                }

                if (coffFile.Symbols.Count > 10)
                {
                    result.AppendLine($"  ... 等 {coffFile.Symbols.Count - 10} 个符号");
                }
            }

            result.AppendLine();

            result.AppendLine($"🔄 重定位数量: {coffFile.Relocations.Count}");
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
            0x0000 => "未知",
            0x014C => "x86 (Intel 386)",
            0x0200 => "IA64 (Intel Itanium)",
            0x8664 => "x64 (AMD64)",
            0x01C0 => "ARM",
            0xAA64 => "ARM64",
            0xEBC => "EFI Byte Code",
            _ => $"未知 (0x{machine:X4})"
        };
    }
}
