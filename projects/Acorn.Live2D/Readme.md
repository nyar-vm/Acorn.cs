# Acorn.Live2D

Live2D Cubism 模型格式的扫描、编码和解码库。

## 格式规范参考

- [Live2D Cubism SDK](https://www.live2d.com/download/cubism-sdk/download-native/)
- [Live2D Cubism Model Format (moc3)](https://cubism.live2d.com/sdk-doc/native/structCubismModelSettingJson.html)
- [CubismMotionJson Reference](https://cubism.live2d.com/sdk-doc/native/)

## 二进制格式结构

### MOC3 文件布局

```
┌──────────────────────────┐
│ Header                    │
│   magic: "MOC3"          │
│   version: uint8[3]       │
│   isBigEndian: uint8      │
│   ...                     │
├──────────────────────────┤
│ Count Section             │
│   Canvas Width/Height     │
│   Part Count              │
│   Parameter Count         │
│   ...                     │
├──────────────────────────┤
│ Offset Section            │
│   ...                     │
├──────────────────────────┤
│ Data Section              │
│   ...                     │
└──────────────────────────┘
```

### Model3.json 布局

Live2D 模型使用 JSON 描述文件关联各资源（moc3、纹理、动作、表情等）。

## API

```csharp
using Acorn.Live2D.Decode;
using Acorn.Live2D.Encode;
using Acorn.Live2D.Scanner;

// 解码
var decoder = new Live2DDecoder();
var model = decoder.Decode(File.ReadAllBytes("model.moc3"));

// 编码
var encoder = new Live2DEncoder();
var bytes = encoder.Encode(model);

// 扫描
var data = File.ReadAllBytes("model.moc3");
var scanner = new Live2DScanner(data);
var header = scanner.ScanHeader();
```
