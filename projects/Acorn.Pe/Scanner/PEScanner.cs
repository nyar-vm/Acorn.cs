using System.Text;
using Acorn.Pe.Data;
using Acorn.Pe.Decode;

namespace Acorn.Pe.Scanner;

/// <summary>
///     PE 文件扫描器，提供对 Windows 可执行文件的快速结构扫描。
/// </summary>
public class PeScanner
{
    /// <summary>
    ///     扫描 PE 文件，提取结构信息。
    /// </summary>
    /// <param name="data">PE 二进制数据。</param>
    /// <returns>扫描结果。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("PE File Scan Result:");
        result.AppendLine("===================");

        if (data.Length < 2)
        {
            result.AppendLine("❌ 文件数据过短");
            return result.ToString();
        }

        if (data[0] != PeConstants.DosMagicBytes[0] || data[1] != PeConstants.DosMagicBytes[1])
        {
            result.AppendLine("❌ 不是有效的 PE 文件");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 PE 文件");
        result.AppendLine();

        try
        {
            var decoder = new PeDecoder();
            var peFile = decoder.Decode(data);

            var fileType = peFile.IsDll ? "📦 DLL" : "🚀 可执行文件 (EXE)";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = peFile.Is64Bit ? "x64 (64位)" : "x86 (32位)";
            result.AppendLine($"架构: {architecture}");

            var machineType = GetMachineTypeName(peFile.Header.Machine);
            result.AppendLine($"机器类型: {machineType}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {peFile.Header.NumberOfSections}");
            if (peFile.Sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in peFile.Sections)
                {
                    result.AppendLine($"  📄 {section.Name}");
                    result.AppendLine($"     虚拟大小: {section.VirtualSize} bytes, 虚拟地址: 0x{section.VirtualAddress:X8}");
                    result.AppendLine($"     原始大小: {section.SizeOfRawData} bytes, 原始地址: 0x{section.PointerToRawData:X8}");
                }
            }

            result.AppendLine();

            if (peFile.OptionalHeader.Magic != 0)
            {
                result.AppendLine("可选头信息:");
                result.AppendLine($"  入口点: 0x{peFile.OptionalHeader.AddressOfEntryPoint:X8}");
                result.AppendLine($"  镜像基址: 0x{peFile.OptionalHeader.ImageBase:X16}");
                result.AppendLine($"  镜像大小: {peFile.OptionalHeader.SizeOfImage} bytes");
                result.AppendLine($"  子系统: {GetSubsystemName(peFile.OptionalHeader.Subsystem)}");
            }
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
            0x014C => "x86 (Intel 386)",
            0x0200 => "IA64 (Intel Itanium)",
            0x8664 => "x64 (AMD64)",
            0x01C0 => "ARM",
            0xAA64 => "ARM64",
            _ => $"未知 (0x{machine:X4})"
        };
    }

    /// <summary>
    ///     获取子系统名称。
    /// </summary>
    private static string GetSubsystemName(ushort subsystem)
    {
        return subsystem switch
        {
            1 => "Native (驱动程序)",
            2 => "Windows GUI",
            3 => "Windows CUI (控制台)",
            5 => "OS/2 CUI",
            7 => "POSIX CUI",
            9 => "Windows CE GUI",
            10 => "EFI 应用程序",
            11 => "EFI 驱动程序 (带启动服务)",
            12 => "EFI 驱动程序 (运行时驱动)",
            13 => "EFI ROM 镜像",
            14 => "Xbox",
            16 => "Windows Boot 应用程序",
            _ => $"未知 ({subsystem})"
        };
    }
}
