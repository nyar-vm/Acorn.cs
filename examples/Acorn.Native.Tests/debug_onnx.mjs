// 调试脚本：检查 Onnx 编码器输出字节
// 用法: node --experimental-vm-modules debug_onnx.mjs

import { execSync } from 'child_process';
import { writeFileSync, readFileSync } from 'fs';

const testCode = `
using Acorn.Onnx.Data;
using Acorn.Onnx.Encode;

var data = new OnnxModelData
{
    IrVersion = 8,
    OpsetImport =
    [
        new OnnxOperatorSetId { Domain = "ai.onnx", Version = 18 }
    ]
};

var bytes = OnnxEncoder.Encode(data);
Console.WriteLine($"Length: {bytes.Length}");
Console.WriteLine($"Hex: {BitConverter.ToString(bytes)}");

// 为每个字节打印详情
for (int i = 0; i < bytes.Length; i++)
{
    Console.WriteLine($"  [{i}] = 0x{bytes[i]:X2} ({bytes[i]})");
}
`;

writeFileSync('e:/RiderProjects/Acorn.cs/examples/Acorn.Native.Tests/_debug_onxx.cs', testCode);

// 直接运行现有的 dotnet test 项目来编码
console.log('Test code written. Use dotnet to compile and check encoder output.');
