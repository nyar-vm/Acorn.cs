# Acorn.Psd

Adobe PSD (Photoshop Document) 图像格式的扫描、编码和解码库。

## 格式规范参考

- [Adobe PSD File Format Specification](https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/)
- [PSB (Large Document Format)](https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/PhotoshopFileFormats.htm#50577409_17587)
- [PSD Specification (Agnostic Dev)](https://github.com/agnostic-apes/psd-specification)

## 二进制格式结构

### PSD 文件布局

```
┌──────────────────────────────────┐
│ File Header (26 bytes)           │
│   signature: "8BPS"              │
│   version: uint16 (1=PSD, 2=PSB) │
│   reserved: byte[6]              │
│   channels: uint16               │
│   height: uint32                 │
│   width: uint32                  │
│   depth: uint16 (1/8/16/32)     │
│   color_mode: uint16             │
├──────────────────────────────────┤
│ Color Mode Data Section          │
│   length: uint32                 │
│   data: byte[]                   │
├──────────────────────────────────┤
│ Image Resources Section          │
│   length: uint32                 │
│   resources: ImageResource[]     │
├──────────────────────────────────┤
│ Layer and Mask Information       │
│   length: uint32                 │
│   layer_info: LayerInfo          │
│   global_layer_mask: byte[]      │
├──────────────────────────────────┤
│ Image Data Section               │
│   compression: uint16            │
│   data: byte[]                   │
└──────────────────────────────────┘
```

## API

```csharp
using Acorn.Psd.Decode;
using Acorn.Psd.Encode;
using Acorn.Psd.Scanner;

// 解码
var decoder = new PsdDecoder();
var image = decoder.Decode(File.ReadAllBytes("image.psd"));

// 编码
var encoder = new PsdEncoder();
var bytes = encoder.Encode(image);

// 扫描
var scanner = new PsdScanner(File.ReadAllBytes("image.psd"));
var header = scanner.ScanHeader();
```
