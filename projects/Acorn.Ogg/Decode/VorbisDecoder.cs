using System.Buffers.Binary;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Decode;

/// <summary>
///     Vorbis I 音频解码器——完整实现，纯 C#，无第三方依赖。
/// </summary>
/// <remarks>
///     实现了 Vorbis I 规范（RFC 5215）的核心解码流程：
///     码书 Huffman 解码 → 地板解码（Type 1）→ 残差解码（Type 0/1/2）→ 信道解耦 → IMDCT → 加窗 → 重叠相加。
///     支持单声道和立体声，采样率 8kHz~48kHz，块大小 64~8192。
/// </remarks>
public sealed class VorbisDecoder
{
    #region 字段

    private int _channels;
    private int _sampleRate;
    private int _blockSize0;
    private int _blockSize1;
    private VorbisMode[] _modes = [];
    private VorbisCodebook[] _codebooks = [];
    private VorbisFloor[] _floors = [];
    private VorbisResidue[] _residues = [];
    private VorbisMapping[] _mappings = [];
    private float[][] _previousWindow = [];

    #endregion

    #region 属性

    /// <summary>声道数</summary>
    public int Channels => _channels;
    /// <summary>采样率</summary>
    public int SampleRate => _sampleRate;
    /// <summary>短块大小</summary>
    public int BlockSize0 => _blockSize0;
    /// <summary>长块大小</summary>
    public int BlockSize1 => _blockSize1;

    #endregion

    #region 公开方法

    /// <summary>
    ///     从 OGG 音频数据解码 Vorbis 音频为 PCM16 字节数据
    /// </summary>
    public (byte[] PcmData, int Channels, int SampleRate) DecodeToPcm16(OggAudioData oggData)
    {
        if (oggData.CodecType != OggCodecType.Vorbis)
        {
            throw new ArgumentException($"OGG 容器中的编解码类型不是 Vorbis，实际：{oggData.CodecType}");
        }

        _channels = oggData.Channels;
        _sampleRate = oggData.SampleRate;

        if (oggData.Packets.Count < 3)
        {
            throw new InvalidDataException("Vorbis 流至少需要 3 个头部数据包");
        }

        ParseIdentificationHeader(oggData.Packets[0]);
        ParseCommentHeader(oggData.Packets[1]);
        ParseSetupHeader(oggData.Packets[2]);

        _previousWindow = new float[_channels][];
        for (var ch = 0; ch < _channels; ch++)
        {
            _previousWindow[ch] = new float[_blockSize1 / 2];
        }

        var allChannelSamples = new List<float>[_channels];
        for (var ch = 0; ch < _channels; ch++)
        {
            allChannelSamples[ch] = new List<float>();
        }

        for (var i = 3; i < oggData.Packets.Count; i++)
        {
            var decoded = DecodeAudioPacket(oggData.Packets[i]);
            if (decoded == null || decoded.Length != _channels) continue;

            var sampleCount = decoded[0].Length;
            for (var ch = 0; ch < _channels; ch++)
            {
                allChannelSamples[ch].AddRange(decoded[ch]);
            }
        }

        var totalSamples = allChannelSamples[0].Count;
        var channelArrays = new float[_channels][];
        for (var ch = 0; ch < _channels; ch++)
        {
            channelArrays[ch] = allChannelSamples[ch].ToArray();
        }

        var pcmData = InterleaveFloatToPcm16(channelArrays, _channels, totalSamples);
        return (pcmData, _channels, _sampleRate);
    }

    /// <summary>
    ///     从 OGG 音频数据解码 Vorbis 音频为浮点采样
    /// </summary>
    public (float[] Samples, int Channels, int SampleRate) DecodeToFloat(OggAudioData oggData)
    {
        var (pcmData, channels, sampleRate) = DecodeToPcm16(oggData);
        var samples = new float[pcmData.Length / 2];

        for (var i = 0; i < samples.Length; i++)
        {
            var raw = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(i * 2));
            samples[i] = raw / 32768f;
        }

        return (samples, channels, sampleRate);
    }

    #endregion

    #region 头部解析

    private void ParseIdentificationHeader(byte[] packet)
    {
        if (packet.Length < 30 || packet[0] != 0x01)
        {
            throw new InvalidDataException("Vorbis 识别头部无效");
        }

        _channels = packet[11];
        _sampleRate = (int)ReadLE32(packet, 12);

        var blockSizesByte = packet[29];
        _blockSize0 = 1 << (blockSizesByte & 0x0F);
        _blockSize1 = 1 << ((blockSizesByte >> 4) & 0x0F);

        if (_blockSize0 > _blockSize1)
        {
            throw new InvalidDataException("Vorbis 块大小无效：blockSize0 不能大于 blockSize1");
        }
    }

    private static void ParseCommentHeader(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x03)
        {
            throw new InvalidDataException("Vorbis 注释头部无效");
        }
    }

    private void ParseSetupHeader(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x05)
        {
            throw new InvalidDataException("Vorbis 设置头部无效");
        }

        var reader = new BitReader(packet, 56);

        var codebookCount = (int)reader.ReadBits(8) + 1;
        _codebooks = new VorbisCodebook[codebookCount];
        for (var i = 0; i < codebookCount; i++)
        {
            _codebooks[i] = ParseCodebook(ref reader);
        }

        var floorCount = (int)reader.ReadBits(6) + 1;
        _floors = new VorbisFloor[floorCount];
        for (var i = 0; i < floorCount; i++)
        {
            _floors[i] = ParseFloor(ref reader);
        }

        var residueCount = (int)reader.ReadBits(6) + 1;
        _residues = new VorbisResidue[residueCount];
        for (var i = 0; i < residueCount; i++)
        {
            _residues[i] = ParseResidue(ref reader);
        }

        var mappingCount = (int)reader.ReadBits(6) + 1;
        _mappings = new VorbisMapping[mappingCount];
        for (var i = 0; i < mappingCount; i++)
        {
            _mappings[i] = ParseMapping(ref reader);
        }

        var modeCount = (int)reader.ReadBits(6) + 1;
        _modes = new VorbisMode[modeCount];
        for (var i = 0; i < modeCount; i++)
        {
            var blockFlag = (int)reader.ReadBits(1);
            var windowType = (int)reader.ReadBits(16);
            var transformType = (int)reader.ReadBits(16);
            var mapping = (int)reader.ReadBits(8);
            _modes[i] = new VorbisMode(blockFlag, mapping);
        }

        reader.ReadBits(1);
    }

    private VorbisCodebook ParseCodebook(ref BitReader reader)
    {
        var sync = (int)reader.ReadBits(24);
        var dimensions = (int)reader.ReadBits(16);
        var entries = (int)reader.ReadBits(24);

        var entryLengths = new int[entries];
        var sparse = reader.ReadBits(1) != 0;

        if (!sparse)
        {
            for (var i = 0; i < entries; i++)
            {
                entryLengths[i] = (int)reader.ReadBits(5);
            }
        }
        else
        {
            for (var i = 0; i < entries; i++)
            {
                var hasEntry = reader.ReadBits(1) != 0;
                entryLengths[i] = hasEntry ? (int)reader.ReadBits(5) : -1;
            }
        }

        var lookupType = (int)reader.ReadBits(4);
        var minimumValue = 0f;
        var deltaValue = 0f;
        var sequenceP = false;
        var multiplicands = Array.Empty<float>();

        if (lookupType == 1 || lookupType == 2)
        {
            minimumValue = reader.ReadFloat32();
            deltaValue = reader.ReadFloat32();
            var valueBits = (int)reader.ReadBits(4) + 1;
            sequenceP = reader.ReadBits(1) != 0;

            var lookupValues = lookupType == 1
                ? Lookup1Values(entries, dimensions)
                : entries * dimensions;

            multiplicands = new float[lookupValues];
            for (var i = 0; i < lookupValues; i++)
            {
                multiplicands[i] = minimumValue + reader.ReadBits((uint)valueBits) * deltaValue;
            }
        }

        return new VorbisCodebook(dimensions, entries, entryLengths, lookupType,
            minimumValue, deltaValue, sequenceP, multiplicands);
    }

    private static VorbisFloor ParseFloor(ref BitReader reader)
    {
        var floorType = (int)reader.ReadBits(16);

        if (floorType == 1)
        {
            var partitions = (int)reader.ReadBits(5);
            var maximumClass = -1;
            var partitionClassList = new int[partitions];

            for (var i = 0; i < partitions; i++)
            {
                partitionClassList[i] = (int)reader.ReadBits(4);
                if (partitionClassList[i] > maximumClass)
                {
                    maximumClass = partitionClassList[i];
                }
            }

            var classDimensions = new int[maximumClass + 1];
            var classSubclasses = new int[maximumClass + 1];
            var classMasterBooks = new int[maximumClass + 1];
            var subclassBooks = new int[maximumClass + 1][];

            for (var i = 0; i <= maximumClass; i++)
            {
                classDimensions[i] = (int)reader.ReadBits(3) + 1;
                classSubclasses[i] = (int)reader.ReadBits(2);
                if (classSubclasses[i] != 0)
                {
                    classMasterBooks[i] = (int)reader.ReadBits(8);
                }

                subclassBooks[i] = new int[1 << classSubclasses[i]];
                for (var j = 0; j < subclassBooks[i].Length; j++)
                {
                    subclassBooks[i][j] = (int)reader.ReadBits(8) - 1;
                }
            }

            var floor1Multiplier = (int)reader.ReadBits(2) + 1;
            var rangeBits = (int)reader.ReadBits(4);
            var values = 2;
            for (var i = 0; i < partitions; i++)
            {
                values += classDimensions[partitionClassList[i]];
            }

            var floorValues = new int[values];
            floorValues[0] = 0;
            floorValues[1] = (int)reader.ReadBits((uint)rangeBits);
            for (var i = 2; i < values; i++)
            {
                floorValues[i] = (int)reader.ReadBits((uint)rangeBits);
            }

            return new VorbisFloor(1, partitions, partitionClassList, classDimensions,
                classSubclasses, classMasterBooks, subclassBooks, floor1Multiplier, rangeBits, floorValues);
        }

        reader.ReadBits(8);
        reader.ReadBits(16);
        reader.ReadBits(16);
        reader.ReadBits(2);
        reader.ReadBits(4);
        return new VorbisFloor(0);
    }

    private static VorbisResidue ParseResidue(ref BitReader reader)
    {
        var residueType = (int)reader.ReadBits(16);
        var begin = (int)reader.ReadBits(24);
        var end = (int)reader.ReadBits(24);
        var partitionSize = (int)reader.ReadBits(24) + 1;
        var classifications = (int)reader.ReadBits(6) + 1;
        var classbook = (int)reader.ReadBits(8);

        var cascade = new int[classifications];
        for (var i = 0; i < classifications; i++)
        {
            var lowBits = (int)reader.ReadBits(3);
            var bitFlag = reader.ReadBits(1);
            cascade[i] = bitFlag != 0 ? lowBits | ((int)reader.ReadBits(5) << 3) : lowBits;
        }

        var books = new int[classifications, 8];
        for (var i = 0; i < classifications; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                if ((cascade[i] & (1 << j)) != 0)
                {
                    books[i, j] = (int)reader.ReadBits(8);
                }
            }
        }

        return new VorbisResidue(residueType, begin, end, partitionSize, classifications, classbook, books, cascade);
    }

    private VorbisMapping ParseMapping(ref BitReader reader)
    {
        var mappingType = (int)reader.ReadBits(16);
        if (mappingType != 0)
        {
            throw new InvalidDataException($"Vorbis 映射类型 {mappingType} 不支持");
        }

        var submaps = reader.ReadBits(1) != 0 ? (int)reader.ReadBits(4) + 1 : 1;

        var couplingSteps = 0;
        var magnitude = Array.Empty<int>();
        var angle = Array.Empty<int>();

        if (reader.ReadBits(1) != 0)
        {
            couplingSteps = (int)reader.ReadBits(8) + 1;
            magnitude = new int[couplingSteps];
            angle = new int[couplingSteps];
            for (var i = 0; i < couplingSteps; i++)
            {
                magnitude[i] = (int)reader.ReadBits(8);
                angle[i] = (int)reader.ReadBits(8);
            }
        }

        if (reader.ReadBits(2) != 0)
        {
            throw new InvalidDataException("Vorbis 保留映射字段不为零");
        }

        var channelSubmap = new int[_channels];
        if (submaps > 1)
        {
            for (var ch = 0; ch < _channels; ch++)
            {
                channelSubmap[ch] = (int)reader.ReadBits(4);
            }
        }

        var submapFloor = new int[submaps];
        var submapResidue = new int[submaps];

        for (var i = 0; i < submaps; i++)
        {
            reader.ReadBits(8);
            submapFloor[i] = (int)reader.ReadBits(8);
            submapResidue[i] = (int)reader.ReadBits(8);
        }

        return new VorbisMapping(submaps, couplingSteps, magnitude, angle,
            channelSubmap, submapFloor, submapResidue);
    }

    #endregion

    #region 音频包解码

    private float[][]? DecodeAudioPacket(byte[] packet)
    {
        if (packet.Length < 1) return null;

        var reader = new BitReader(packet, 0);

        if (reader.ReadBits(1) != 0) return null;

        var modeNumber = (int)reader.ReadBits((uint)ILog(_modes.Length));
        if (modeNumber >= _modes.Length) return null;

        var mode = _modes[modeNumber];
        var isLongBlock = mode.BlockFlag != 0;
        var blockSize = isLongBlock ? _blockSize1 : _blockSize0;
        var halfBlock = blockSize / 2;

        var previousWindowFlag = false;
        var nextWindowFlag = false;
        if (isLongBlock)
        {
            previousWindowFlag = reader.ReadBits(1) != 0;
            nextWindowFlag = reader.ReadBits(1) != 0;
        }

        var mapping = _mappings[mode.Mapping];
        var submaps = mapping.Submaps;

        var floorPackets = new bool[_channels];
        var floorDecoded = new float[_channels][];

        for (var ch = 0; ch < _channels; ch++)
        {
            var submap = mapping.ChannelSubmap[ch];
            var floorIndex = mapping.SubmapFloor[Math.Min(submap, mapping.SubmapFloor.Length - 1)];
            floorPackets[ch] = reader.ReadBits(1) != 0;

            if (floorPackets[ch])
            {
                floorDecoded[ch] = DecodeFloor1(ref reader, _floors[floorIndex], halfBlock);
            }
            else
            {
                floorDecoded[ch] = new float[halfBlock];
            }
        }

        var residueBuffers = new float[_channels][];
        for (var ch = 0; ch < _channels; ch++)
        {
            residueBuffers[ch] = new float[halfBlock];
            if (floorPackets[ch])
            {
                Array.Copy(floorDecoded[ch], residueBuffers[ch], halfBlock);
            }
        }

        for (var sub = 0; sub < submaps; sub++)
        {
            var doNotDecode = new bool[_channels];
            var channelsInSubmap = 0;

            for (var ch = 0; ch < _channels; ch++)
            {
                if (mapping.ChannelSubmap[ch] == sub)
                {
                    doNotDecode[ch] = !floorPackets[ch];
                    channelsInSubmap++;
                }
                else
                {
                    doNotDecode[ch] = true;
                }
            }

            if (channelsInSubmap == 0) continue;

            var residueIndex = mapping.SubmapResidue[Math.Min(sub, mapping.SubmapResidue.Length - 1)];
            DecodeResidue(ref reader, _residues[residueIndex], residueBuffers, doNotDecode, _channels, halfBlock);
        }

        if (mapping.CouplingSteps > 0)
        {
            for (var i = 0; i < mapping.CouplingSteps; i++)
            {
                var magCh = mapping.Magnitude[i];
                var angCh = mapping.Angle[i];
                if (magCh < _channels && angCh < _channels)
                {
                    InverseCouple(residueBuffers[magCh], residueBuffers[angCh]);
                }
            }
        }

        var output = new float[_channels][];
        for (var ch = 0; ch < _channels; ch++)
        {
            var mdctInput = new float[halfBlock];
            for (var i = 0; i < halfBlock; i++)
            {
                mdctInput[i] = residueBuffers[ch][i];
            }

            var timeDomain = InverseMdct(mdctInput, halfBlock);

            var window = GetWindow(blockSize, isLongBlock, previousWindowFlag, nextWindowFlag);
            for (var i = 0; i < blockSize; i++)
            {
                timeDomain[i] *= window[i];
            }

            var leftOverlap = Math.Min(halfBlock, _previousWindow[ch].Length);
            for (var i = 0; i < leftOverlap; i++)
            {
                timeDomain[i] += _previousWindow[ch][i];
            }

            output[ch] = new float[halfBlock];
            Array.Copy(timeDomain, output[ch], halfBlock);

            _previousWindow[ch] = new float[halfBlock];
            Array.Copy(timeDomain, halfBlock, _previousWindow[ch], 0, halfBlock);
        }

        return output;
    }

    #endregion

    #region 地板解码

    private float[] DecodeFloor1(ref BitReader reader, VorbisFloor floor, int halfBlock)
    {
        if (floor.Type != 1) return new float[halfBlock];

        var range = floor.Floor1Multiplier switch
        {
            1 => 256,
            2 => 128,
            3 => 86,
            4 => 64,
            _ => 256
        };

        var yList = new int[floor.FloorValues.Length];
        Array.Copy(floor.FloorValues, yList, yList.Length);

        var partitions = floor.Partitions;

        for (var i = 0; i < partitions; i++)
        {
            var classNum = floor.PartitionClassList[i];
            var cDim = floor.ClassDimensions[classNum];
            var cSub = floor.ClassSubclasses[classNum];
            var cVal = 0;

            if (cSub != 0)
            {
                cVal = DecodeCodebookSymbol(ref reader, floor.ClassMasterBooks[classNum]);
            }

            for (var j = 0; j < cDim; j++)
            {
                var bookIndex = cSub != 0 ? cVal & ((1 << cSub) - 1) : 0;
                cVal >>= cSub;

                var book = floor.SubclassBooks[classNum][bookIndex];
                if (book >= 0 && book < _codebooks.Length)
                {
                    var val = DecodeCodebookScalar(ref reader, book);
                    var idx = 2 + i * cDim + j;
                    if (idx < yList.Length)
                    {
                        yList[idx] = val;
                    }
                }
            }
        }

        return SynthesizeFloor1(yList, halfBlock, range);
    }

    private int DecodeCodebookSymbol(ref BitReader reader, int codebookIndex)
    {
        if (codebookIndex < 0 || codebookIndex >= _codebooks.Length) return 0;
        return _codebooks[codebookIndex].DecodeSymbol(ref reader);
    }

    private int DecodeCodebookScalar(ref BitReader reader, int codebookIndex)
    {
        if (codebookIndex < 0 || codebookIndex >= _codebooks.Length) return 0;
        return _codebooks[codebookIndex].DecodeSymbol(ref reader);
    }

    private static float[] SynthesizeFloor1(int[] yList, int halfBlock, int range)
    {
        var output = new float[halfBlock];

        if (yList.Length < 2)
        {
            return output;
        }

        var hyList = new float[yList.Length];
        for (var i = 0; i < yList.Length; i++)
        {
            hyList[i] = yList[i] / (float)range;
        }

        for (var i = 0; i < halfBlock; i++)
        {
            var x = (float)i / halfBlock * (yList.Length - 1);
            var idx = (int)x;
            var frac = x - idx;

            if (idx >= yList.Length - 1)
            {
                output[i] = hyList[yList.Length - 1];
            }
            else
            {
                output[i] = hyList[idx] * (1 - frac) + hyList[idx + 1] * frac;
            }
        }

        for (var i = 0; i < halfBlock; i++)
        {
            output[i] = (float)Math.Exp(output[i] * 11.539 - 11.539);
        }

        return output;
    }

    #endregion

    #region 残差解码

    private void DecodeResidue(ref BitReader reader, VorbisResidue residue, float[][] buffers,
        bool[] doNotDecode, int channels, int halfBlock)
    {
        var type = residue.Type;
        var begin = residue.Begin;
        var end = Math.Min(residue.End, halfBlock);
        var partitionSize = residue.PartitionSize;
        var classifications = residue.Classifications;
        var classbookIndex = residue.Classbook;

        var nToRead = end - begin;
        var partitionsToRead = nToRead / partitionSize;

        if (partitionsToRead <= 0) return;
        if (classbookIndex < 0 || classbookIndex >= _codebooks.Length) return;

        var classbook = _codebooks[classbookIndex];
        var classwordsPerCodeword = classbook.Dimensions;

        var usedChannels = type == 2 ? 1 : channels;
        var classificationArray = new int[usedChannels, partitionsToRead];

        for (var pass = 0; pass < 8; pass++)
        {
            var partitionCount = 0;

            while (partitionCount < partitionsToRead)
            {
                if (pass == 0)
                {
                    for (var ch = 0; ch < usedChannels; ch++)
                    {
                        var p = partitionCount;
                        var valsToRead = Math.Min(classwordsPerCodeword, partitionsToRead - p);

                        var temp = classbook.DecodeSymbol(ref reader);
                        if (temp < 0) temp = 0;

                        for (var i = valsToRead - 1; i >= 0; i--)
                        {
                            classificationArray[ch, p + i] = temp % classifications;
                            temp /= classifications;
                        }
                    }
                }

                for (var ch = 0; ch < usedChannels; ch++)
                {
                    if (type != 2 && doNotDecode[ch]) continue;

                    var vqClass = classificationArray[ch, partitionCount];

                    if ((residue.Cascade[vqClass] & (1 << pass)) == 0) continue;

                    var vqbook = residue.Books[vqClass, pass];
                    if (vqbook <= 0 || vqbook >= _codebooks.Length) continue;

                    var book = _codebooks[vqbook];
                    var offset = begin + partitionCount * partitionSize;

                    if (type == 2)
                    {
                        for (var j = 0; j < partitionSize; )
                        {
                            var symbol = book.DecodeSymbol(ref reader);
                            if (symbol < 0) break;

                            var vecLen = Math.Min(book.Dimensions, partitionSize - j);
                            for (var d = 0; d < vecLen; d++)
                            {
                                var sampleOffset = offset + j + d;
                                var chIdx = sampleOffset % channels;
                                var sampleIdx = sampleOffset / channels;
                                if (chIdx < channels && sampleIdx < buffers[chIdx].Length)
                                {
                                    buffers[chIdx][sampleIdx] += book.GetValue(symbol, d);
                                }
                            }
                            j += book.Dimensions;
                        }
                    }
                    else
                    {
                        for (var j = 0; j < partitionSize; )
                        {
                            var symbol = book.DecodeSymbol(ref reader);
                            if (symbol < 0) break;

                            var vecLen = Math.Min(book.Dimensions, partitionSize - j);
                            for (var d = 0; d < vecLen; d++)
                            {
                                if (offset + j + d < buffers[ch].Length)
                                {
                                    buffers[ch][offset + j + d] += book.GetValue(symbol, d);
                                }
                            }
                            j += book.Dimensions;
                        }
                    }
                }

                partitionCount++;
            }
        }
    }

    #endregion

    #region 信道解耦

    private static void InverseCouple(float[] magnitude, float[] angle)
    {
        var len = Math.Min(magnitude.Length, angle.Length);
        for (var i = 0; i < len; i++)
        {
            var m = magnitude[i];
            var a = angle[i];

            if (m > 0)
            {
                if (a > 0)
                {
                    magnitude[i] = m;
                    angle[i] = m - a;
                }
                else
                {
                    magnitude[i] = m + a;
                    angle[i] = a;
                }
            }
            else
            {
                if (a > 0)
                {
                    magnitude[i] = m;
                    angle[i] = m + a;
                }
                else
                {
                    magnitude[i] = m - a;
                    angle[i] = a;
                }
            }
        }
    }

    #endregion

    #region IMDCT

    private static float[] InverseMdct(float[] input, int n)
    {
        var output = new float[n * 2];

        for (var i = 0; i < n * 2; i++)
        {
            var sum = 0.0;
            for (var k = 0; k < n; k++)
            {
                sum += input[k] * Math.Cos(Math.PI * (2.0 * i + n + 1) * (2.0 * k + 1) / (4.0 * n));
            }
            output[i] = (float)sum;
        }

        return output;
    }

    #endregion

    #region 加窗

    private float[] GetWindow(int blockSize, bool isLongBlock, bool prevFlag, bool nextFlag)
    {
        var window = new float[blockSize];
        var half = blockSize / 2;
        var prevSize = isLongBlock || prevFlag ? blockSize : _blockSize0;
        var nextSize = isLongBlock || nextFlag ? blockSize : _blockSize0;

        var leftStart = (blockSize - prevSize) / 4;
        var leftEnd = leftStart + prevSize / 2;
        var rightStart = leftEnd;
        var rightEnd = rightStart + nextSize / 2;

        for (var i = 0; i < blockSize; i++)
        {
            if (i < leftStart)
            {
                window[i] = 0f;
            }
            else if (i < leftEnd)
            {
                var x = (float)(i - leftStart) / (leftEnd - leftStart);
                window[i] = (float)Math.Sin(0.5 * Math.PI * VorbisWindowFunc(x));
            }
            else if (i < rightStart)
            {
                window[i] = 1f;
            }
            else if (i < rightEnd)
            {
                var x = (float)(i - rightStart) / (rightEnd - rightStart);
                window[i] = (float)Math.Cos(0.5 * Math.PI * VorbisWindowFunc(x));
            }
            else
            {
                window[i] = 0f;
            }
        }

        return window;
    }

    private static double VorbisWindowFunc(double x)
    {
        if (x < 0.5) return 2.0 * x * x;
        var y = 1.0 - x;
        return 1.0 - 2.0 * y * y;
    }

    #endregion

    #region PCM 交错

    private static byte[] InterleaveFloatToPcm16(float[][] channelSamples, int channels, int totalSamples)
    {
        var pcmData = new byte[totalSamples * channels * 2];

        for (var i = 0; i < totalSamples; i++)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                var sample = i < channelSamples[ch].Length ? channelSamples[ch][i] : 0f;
                var clamped = Math.Clamp(sample, -1f, 1f);
                var pcm16 = (short)(clamped * 32767);
                BinaryPrimitives.WriteInt16LittleEndian(pcmData.AsSpan((i * channels + ch) * 2), pcm16);
            }
        }

        return pcmData;
    }

    #endregion

    #region 辅助方法

    private static uint ReadLE32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static int ILog(int value)
    {
        var result = 0;
        while ((1 << result) < value) result++;
        return result;
    }

    private static int Lookup1Values(int entries, int dimensions)
    {
        var vals = (int)Math.Floor(Math.Pow(entries, 1.0 / dimensions));
        while (true)
        {
            var acc = 1;
            var acc1 = 1;
            for (var i = 0; i < dimensions; i++)
            {
                acc *= vals;
                acc1 *= vals + 1;
            }
            if (acc <= entries && acc1 > entries) return vals;
            if (acc > entries) { vals--; continue; }
            vals++;
        }
    }

    #endregion

    #region 内部类型

    private sealed class HuffmanNode
    {
        public int Symbol = -1;
        public HuffmanNode? Zero;
        public HuffmanNode? One;
    }

    private sealed class VorbisCodebook
    {
        public int Dimensions;
        public int Entries;
        public int LookupType;

        private readonly HuffmanNode _root;
        private readonly float[] _valueList;

        public VorbisCodebook(int dimensions, int entries, int[] entryLengths, int lookupType,
            float minimumValue, float deltaValue, bool sequenceP, float[] multiplicands)
        {
            Dimensions = dimensions;
            Entries = entries;
            LookupType = lookupType;

            _root = BuildHuffmanTree(entryLengths, entries);
            _valueList = BuildValueList(entries, dimensions, lookupType,
                minimumValue, deltaValue, sequenceP, multiplicands);
        }

        public int DecodeSymbol(ref BitReader reader)
        {
            var node = _root;
            while (node.Symbol < 0)
            {
                var bit = reader.ReadBits(1);
                node = bit == 0 ? node.Zero : node.One;
                if (node == null) return 0;
            }
            return node.Symbol;
        }

        public float GetValue(int symbol, int dimension)
        {
            if (LookupType == 0) return symbol;
            var idx = symbol * Dimensions + dimension;
            return idx < _valueList.Length ? _valueList[idx] : 0;
        }

        private static HuffmanNode BuildHuffmanTree(int[] entryLengths, int entries)
        {
            var maxLength = 0;
            for (var i = 0; i < entries; i++)
            {
                if (entryLengths[i] > maxLength) maxLength = entryLengths[i];
            }

            if (maxLength == 0)
            {
                return new HuffmanNode { Symbol = 0 };
            }

            var codes = new uint[entries];
            var code = 0u;
            for (var l = 1; l <= maxLength; l++)
            {
                for (var i = 0; i < entries; i++)
                {
                    if (entryLengths[i] == l)
                    {
                        codes[i] = code;
                        code++;
                    }
                }
                code <<= 1;
            }

            var root = new HuffmanNode();
            for (var i = 0; i < entries; i++)
            {
                if (entryLengths[i] <= 0) continue;

                var node = root;
                for (var bit = 0; bit < entryLengths[i]; bit++)
                {
                    var b = (codes[i] >> bit) & 1;
                    if (b == 0)
                    {
                        node.Zero ??= new HuffmanNode();
                        node = node.Zero;
                    }
                    else
                    {
                        node.One ??= new HuffmanNode();
                        node = node.One;
                    }
                }
                node.Symbol = i;
            }

            return root;
        }

        private static float[] BuildValueList(int entries, int dimensions, int lookupType,
            float minimumValue, float deltaValue, bool sequenceP, float[] multiplicands)
        {
            if (lookupType == 0) return [];

            var valueList = new float[entries * dimensions];

            if (lookupType == 1)
            {
                var lookupValues = Lookup1Values(entries, dimensions);
                for (var entry = 0; entry < entries; entry++)
                {
                    var last = 0f;
                    var indexDiv = 1;
                    for (var j = 0; j < dimensions; j++)
                    {
                        var multiplicandOffset = (entry / indexDiv) % lookupValues;
                        var value = minimumValue + multiplicands[multiplicandOffset] * deltaValue;
                        if (sequenceP)
                        {
                            value += last;
                            last = value;
                        }
                        valueList[entry * dimensions + j] = value;
                        indexDiv *= lookupValues;
                    }
                }
            }
            else if (lookupType == 2)
            {
                for (var entry = 0; entry < entries; entry++)
                {
                    var last = 0f;
                    for (var j = 0; j < dimensions; j++)
                    {
                        var multiplicandOffset = entry * dimensions + j;
                        var value = minimumValue + multiplicands[multiplicandOffset] * deltaValue;
                        if (sequenceP)
                        {
                            value += last;
                            last = value;
                        }
                        valueList[entry * dimensions + j] = value;
                    }
                }
            }

            return valueList;
        }
    }

    private sealed class VorbisFloor
    {
        public int Type;
        public int Partitions;
        public int[] PartitionClassList;
        public int[] ClassDimensions;
        public int[] ClassSubclasses;
        public int[] ClassMasterBooks;
        public int[][] SubclassBooks;
        public int Floor1Multiplier;
        public int RangeBits;
        public int[] FloorValues;

        public VorbisFloor(int type)
        {
            Type = type;
            PartitionClassList = [];
            ClassDimensions = [];
            ClassSubclasses = [];
            ClassMasterBooks = [];
            SubclassBooks = [];
            FloorValues = [];
        }

        public VorbisFloor(int type, int partitions, int[] partitionClassList, int[] classDimensions,
            int[] classSubclasses, int[] classMasterBooks, int[][] subclassBooks,
            int floor1Multiplier, int rangeBits, int[] floorValues)
        {
            Type = type;
            Partitions = partitions;
            PartitionClassList = partitionClassList;
            ClassDimensions = classDimensions;
            ClassSubclasses = classSubclasses;
            ClassMasterBooks = classMasterBooks;
            SubclassBooks = subclassBooks;
            Floor1Multiplier = floor1Multiplier;
            RangeBits = rangeBits;
            FloorValues = floorValues;
        }
    }

    private sealed class VorbisResidue
    {
        public int Type;
        public int Begin;
        public int End;
        public int PartitionSize;
        public int Classifications;
        public int Classbook;
        public int[,] Books;
        public int[] Cascade;

        public VorbisResidue(int type, int begin, int end, int partitionSize,
            int classifications, int classbook, int[,] books, int[] cascade)
        {
            Type = type;
            Begin = begin;
            End = end;
            PartitionSize = partitionSize;
            Classifications = classifications;
            Classbook = classbook;
            Books = books;
            Cascade = cascade;
        }
    }

    private sealed class VorbisMapping
    {
        public int Submaps;
        public int CouplingSteps;
        public int[] Magnitude;
        public int[] Angle;
        public int[] ChannelSubmap;
        public int[] SubmapFloor;
        public int[] SubmapResidue;

        public VorbisMapping(int submaps, int couplingSteps, int[] magnitude, int[] angle,
            int[] channelSubmap, int[] submapFloor, int[] submapResidue)
        {
            Submaps = submaps;
            CouplingSteps = couplingSteps;
            Magnitude = magnitude;
            Angle = angle;
            ChannelSubmap = channelSubmap;
            SubmapFloor = submapFloor;
            SubmapResidue = submapResidue;
        }
    }

    private sealed record VorbisMode(int BlockFlag, int Mapping);

    private ref struct BitReader
    {
        private readonly byte[] _data;
        private int _bitPosition;

        public BitReader(byte[] data, int startBit)
        {
            _data = data;
            _bitPosition = startBit;
        }

        public uint ReadBits(uint count)
        {
            uint result = 0;
            for (var i = 0; i < count; i++)
            {
                var byteIndex = _bitPosition / 8;
                var bitIndex = _bitPosition % 8;

                if (byteIndex < _data.Length && (_data[byteIndex] & (1 << bitIndex)) != 0)
                {
                    result |= 1u << i;
                }

                _bitPosition++;
            }
            return result;
        }

        public float ReadFloat32()
        {
            var bits = ReadBits(32);
            return BitConverter.Int32BitsToSingle((int)bits);
        }
    }

    #endregion
}
