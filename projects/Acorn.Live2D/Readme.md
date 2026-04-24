# 📦 Acorn.Live2D

Live2D Cubism `.moc3` 模型二进制格式编解码器。

## 📐 格式布局

### moc3 文件头

| 字段 | 偏移 | 大小 | 说明 | 对应类 |
|---|---|---|---|---|
| Signature | 0x00 | 4 | `"MOC3"` 签名 | `Live2DConstants.Moc3MagicNumber` |
| Version | 0x04 | 1 | moc3 版本（3/4/5） | `Live2DModelData.Version` |
| Flags | 0x05 | 1 | 标志（大端序位等） | `Live2DModelData.IsBigEndian` |
| Revision | 0x06 | 1 | 版本修订号 | `Live2DModelData.Revision` |
| Padding | 0x07 | 1 | 填充 | - |

### 计数表（Count Table）

| 字段 | 大小 | 说明 | 对应类 |
|---|---|---|---|
| ParameterCount | 4 | 参数数量 | `Live2DModelData.Parameters` |
| PartCount | 4 | 部件数量 | `Live2DModelData.Parts` |
| DrawableCount | 4 | 绘制对象数量 | `Live2DModelData.Drawables` |
| DeformerCount | 4 | 变形器数量 | `Live2DModelData.Deformers` |
| TextureCount | 4 | 纹理数量 | `Live2DModelData.TextureCount` |

### 数据段

| 段 | 说明 | 对应类 |
|---|---|---|
| Canvas | 画布信息（宽度、高度、原点） | `Live2DCanvasInfo` |
| Parameters | 参数定义（ID、最小值、最大值、默认值） | `Live2DParameter` |
| Parts | 部件定义（ID、父部件索引） | `Live2DPart` |
| Drawables | 绘制对象（顶点、索引、UV、绑定参数） | `Live2DDrawable` |
| Deformers | 变形器（类型、位置、绑定参数） | `Live2DDeformer` |

## 🏗️ 核心类

| 类 | 说明 | 文件 |
|---|---|---|
| `Live2DModelData` | moc3 模型完整数据 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DCanvasInfo` | 画布信息 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DParameter` | 参数定义 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DPart` | 部件定义 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DDrawable` | 绘制对象 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DDeformer` | 变形器 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs) |
| `Live2DConstants` | Live2D 常量 | [Data/Live2DConstants.cs](Data/Live2DConstants.cs) |
| `Live2DDecoder` | moc3 解码器 | [Decode/Live2DDecoder.cs](Decode/Live2DDecoder.cs) |
| `Live2DEncoder` | moc3 编码器 | [Encode/Live2DEncoder.cs](Encode/Live2DEncoder.cs) |
| `Live2DScanner` | moc3 扫描器 | [Scanner/Live2DScanner.cs](Scanner/Live2DScanner.cs) |

## 📚 格式规范参考

- [Live2D Cubism SDK 文档](https://docs.live2d.com/cubism-sdk-manual/)
- [Live2D 官方 GitHub](https://github.com/Live2D)
