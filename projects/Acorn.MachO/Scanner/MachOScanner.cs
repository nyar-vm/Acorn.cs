using System.Text;
using Acorn.MachO.Data;
using Acorn.MachO.Decode;

namespace Acorn.MachO.Scanner;

/// <summary>
///     Mach-O 文件扫描器，提供对 macOS/iOS 可执行文件的快速结构扫描。
/// </summary>
public class MachOScanner
{
    /// <summary>
    ///     扫描 Mach-O 文件，提取结构信息。
    /// </summary>
    /// <param name="data">Mach-O 二进制数据。</param>
    /// <returns>扫描结果。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("Mach-O File Scan Result:");
        result.AppendLine("=======================");

        if (data.Length < 4)
        {
            result.AppendLine("❌ 文件数据过短");
            return result.ToString();
        }

        var magic = BitConverter.ToUInt32(data, 0);
        var isValidMagic = magic == 0xFEEDFACE || magic == 0xFEEDFACF ||
                          magic == 0xCEFAEDFE || magic == 0xCFFAEDFE;

        if (!isValidMagic)
        {
            result.AppendLine("❌ 不是有效的 Mach-O 文件");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 Mach-O 文件");
        result.AppendLine();

        try
        {
            var decoder = new MachODecoder();
            var machoFile = decoder.Decode(data);

            var fileType = machoFile.Header.IsExecutable ? "🚀 可执行文件" :
                          machoFile.Header.IsDynamicLibrary ? "📦 动态库 (Dylib)" : "📄 其他";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = machoFile.Header.Is64Bit ? "x64 (64位)" : "x86 (32位)";
            result.AppendLine($"架构: {architecture}");

            var isLittleEndian = magic == 0xFEEDFACE || magic == 0xFEEDFACF;
            var endianness = isLittleEndian ? "小端 (Little Endian)" : "大端 (Big Endian)";
            result.AppendLine($"字节序: {endianness}");

            var cpuType = GetCPUTypeName(machoFile.Header.CPUType);
            result.AppendLine($"CPU 类型: {cpuType}");

            result.AppendLine();

            result.AppendLine($"📦 加载命令数量: {machoFile.Header.NumberOfLoadCommands}");
            result.AppendLine($"📦 加载命令总大小: {machoFile.Header.SizeOfLoadCommands} bytes");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {machoFile.Sections.Count}");
            if (machoFile.Sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in machoFile.Sections)
                {
                    result.AppendLine($"  📄 {section.SectionName}");
                    result.AppendLine($"     段: {section.SegmentName}, 大小: {section.Size} bytes");
                    result.AppendLine($"     虚拟地址: 0x{section.Address:X16}, 文件偏移: 0x{section.Offset:X8}");
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误: {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     获取 CPU 类型名称。
    /// </summary>
    private static string GetCPUTypeName(int cpuType)
    {
        return cpuType switch
        {
            0x00000001 => "VAX",
            0x00000006 => "MC680x0",
            0x00000007 => "x86",
            0x01000007 => "x86_64",
            0x0000000C => "ARM",
            0x0100000C => "ARM64",
            0x00000012 => "PowerPC",
            0x01000012 => "PowerPC_64",
            _ => $"未知 (0x{cpuType:X8})"
        };
    }
}
