# Acorn.Spine

Spine 2D 骨骼动画格式的扫描、编码和解码库。

## 格式规范参考

- [Spine Runtime Documentation](https://en.esotericsoftware.com/spine-runtime-reference)
- [Spine Binary Format](https://en.esotericsoftware.com/spine-binary-format)
- [Spine JSON Format](https://en.esotericsoftware.com/spine-json-format)
- [Spine Atlas Format](https://en.esotericsoftware.com/spine-atlas-format)

## 二进制格式结构

### SKEL 文件布局

```
┌──────────────────────────────────┐
│ Header                           │
│   hash: string (variable)        │
│   version: string (variable)     │
│   width: float                   │
│   height: float                  │
├──────────────────────────────────┤
│ Nonessential Data                │
│   fps: float                     │
│   images: string                 │
├──────────────────────────────────┤
│ Bones Section                    │
│   boneCount: varint             │
│   bones: BoneData[]             │
├──────────────────────────────────┤
│ Slots Section                    │
│   slotCount: varint             │
│   slots: SlotData[]             │
├──────────────────────────────────┤
│ IK Constraints Section           │
│   ikCount: varint               │
│   ...                            │
├──────────────────────────────────┤
│ Skins Section                    │
│   skinCount: varint             │
│   skins: SkinData[]             │
├──────────────────────────────────┤
│ Events Section                   │
│   eventCount: varint            │
│   events: EventData[]           │
├──────────────────────────────────┤
│ Animations Section               │
│   animationCount: varint        │
│   animations: AnimationData[]   │
└──────────────────────────────────┘
```

### Atlas 文件布局

Atlas 是纯文本格式，描述纹理图集的区域划分。

## API

```csharp
using Acorn.Spine.Decode;
using Acorn.Spine.Encode;
using Acorn.Spine.Scanner;

// 解码
var decoder = new SpineDecoder();
var project = decoder.Decode(File.ReadAllBytes("animation.skel"));

// 编码
var encoder = new SpineEncoder();
var bytes = encoder.Encode(project);

// 扫描
var scanner = new SpineScanner(File.ReadAllBytes("animation.skel"));
var header = scanner.ScanHeader();
```
