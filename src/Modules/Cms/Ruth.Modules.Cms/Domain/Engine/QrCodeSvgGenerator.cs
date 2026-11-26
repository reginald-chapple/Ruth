using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>
/// Clean-room ISO/IEC 18004 QR Code (Byte mode, EC Level M, Versions 1–6) deterministic SVG generator.
/// Produces scannable, dependency-free SVG QR codes for CMS shortlinks and signage links.
/// </summary>
public static class QrCodeSvgGenerator
{
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct QrVersionSpec(
        int Version,
        int Size,
        int DataCodewords,
        int EcCodewordsPerBlock,
        int Blocks,
        int AlignmentCenter
    );

    // ISO/IEC 18004 Table 9 — Level M capacities for Versions 1–6
    private static readonly QrVersionSpec[] _versions =
    [
        new(1, 21, 16, 10, 1, 0),
        new(2, 25, 28, 16, 1, 18),
        new(3, 29, 44, 26, 1, 22),
        new(4, 33, 64, 18, 2, 26),
        new(5, 37, 86, 24, 2, 30),
        new(6, 41, 108, 16, 4, 34),
    ];

    // Precomputed BCH(15,5) format bits for Level M (00) + Mask 0 (000) XOR 0x5412 = 0x5412
    private const int FormatInfoLevelMMask0 = 0x5412;

    public static string GenerateSvg(string payload, int quietZoneModules = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var bytes = Encoding.UTF8.GetBytes(payload);
        var spec = SelectVersion(bytes.Length);
        var codewords = BuildCodewords(bytes, spec);
        var matrix = BuildMatrix(spec, codewords);

        return RenderSvg(matrix, spec.Size, Math.Clamp(quietZoneModules, 2, 8));
    }

    private static QrVersionSpec SelectVersion(int byteLength)
    {
        foreach (var v in _versions)
        {
            // Byte mode overhead: 4-bit mode + 8-bit length = 2 bytes
            if (byteLength + 2 <= v.DataCodewords)
            {
                return v;
            }
        }

        return _versions[^1];
    }

    private static byte[] BuildCodewords(byte[] input, QrVersionSpec spec)
    {
        var maxPayload = spec.DataCodewords - 2;
        var len = Math.Min(input.Length, maxPayload);

        var bits = new List<bool>(spec.DataCodewords * 8);
        AppendBits(bits, 0b0100, 4); // Byte mode
        AppendBits(bits, len, 8);
        for (var i = 0; i < len; i++)
        {
            AppendBits(bits, input[i], 8);
        }

        var terminator = Math.Min(4, (spec.DataCodewords * 8) - bits.Count);
        AppendBits(bits, 0, terminator);
        while (bits.Count % 8 != 0)
        {
            bits.Add(false);
        }

        var data = new byte[spec.DataCodewords];
        for (var i = 0; i < bits.Count / 8; i++)
        {
            byte val = 0;
            for (var b = 0; b < 8; b++)
            {
                val = (byte)((val << 1) | (bits[(i * 8) + b] ? 1 : 0));
            }

            data[i] = val;
        }

        byte[] padBytes = [0xEC, 0x11];
        for (int i = bits.Count / 8, p = 0; i < data.Length; i++, p ^= 1)
        {
            data[i] = padBytes[p];
        }

        return InterleaveWithReedSolomon(data, spec);
    }

    private static byte[] InterleaveWithReedSolomon(byte[] data, QrVersionSpec spec)
    {
        var blockDataLen = spec.DataCodewords / spec.Blocks;
        var dataBlocks = new byte[spec.Blocks][];
        var ecBlocks = new byte[spec.Blocks][];
        var generator = BuildReedSolomonGenerator(spec.EcCodewordsPerBlock);

        for (var b = 0; b < spec.Blocks; b++)
        {
            dataBlocks[b] = data[(b * blockDataLen)..((b + 1) * blockDataLen)];
            ecBlocks[b] = ComputeReedSolomonRemainder(dataBlocks[b], generator);
        }

        var result = new byte[spec.DataCodewords + (spec.Blocks * spec.EcCodewordsPerBlock)];
        var idx = 0;
        for (var i = 0; i < blockDataLen; i++)
        {
            for (var b = 0; b < spec.Blocks; b++)
            {
                result[idx++] = dataBlocks[b][i];
            }
        }

        for (var i = 0; i < spec.EcCodewordsPerBlock; i++)
        {
            for (var b = 0; b < spec.Blocks; b++)
            {
                result[idx++] = ecBlocks[b][i];
            }
        }

        return result;
    }

    private static bool[,] BuildMatrix(QrVersionSpec spec, byte[] codewords)
    {
        var size = spec.Size;
        var modules = new bool[size, size];
        var isFunction = new bool[size, size];

        PlaceFinderPattern(modules, isFunction, 0, 0);
        PlaceFinderPattern(modules, isFunction, size - 7, 0);
        PlaceFinderPattern(modules, isFunction, 0, size - 7);
        PlaceTimingAndAlignment(modules, isFunction, spec);
        ReserveFormatAreas(isFunction, size);
        PlaceDataBits(modules, isFunction, size, codewords);
        WriteFormatBits(modules, size, FormatInfoLevelMMask0);

        return modules;
    }

    private static void PlaceFinderPattern(bool[,] modules, bool[,] isFunction, int left, int top)
    {
        var size = modules.GetLength(0);
        for (var dy = -1; dy <= 7; dy++)
        {
            for (var dx = -1; dx <= 7; dx++)
            {
                var x = left + dx;
                var y = top + dy;
                if (x < 0 || y < 0 || x >= size || y >= size)
                {
                    continue;
                }

                var dist = Math.Max(Math.Abs(dx - 3), Math.Abs(dy - 3));
                modules[y, x] = dist is 0 or 1 or 3;
                isFunction[y, x] = true;
            }
        }
    }

    private static void PlaceTimingAndAlignment(
        bool[,] modules,
        bool[,] isFunction,
        QrVersionSpec spec
    )
    {
        var size = spec.Size;
        for (var i = 8; i < size - 8; i++)
        {
            var dark = i % 2 == 0;
            modules[6, i] = dark;
            isFunction[6, i] = true;
            modules[i, 6] = dark;
            isFunction[i, 6] = true;
        }

        modules[size - 8, 8] = true;
        isFunction[size - 8, 8] = true;

        if (spec.AlignmentCenter <= 0)
        {
            return;
        }

        var c = spec.AlignmentCenter;
        for (var dy = -2; dy <= 2; dy++)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                modules[c + dy, c + dx] = dist is 0 or 2;
                isFunction[c + dy, c + dx] = true;
            }
        }
    }

    private static void ReserveFormatAreas(bool[,] isFunction, int size)
    {
        for (var i = 0; i <= 8; i++)
        {
            isFunction[8, i] = true;
            isFunction[i, 8] = true;
        }

        for (var i = 0; i < 8; i++)
        {
            isFunction[8, size - 1 - i] = true;
            isFunction[size - 1 - i, 8] = true;
        }
    }

    private static void PlaceDataBits(
        bool[,] modules,
        bool[,] isFunction,
        int size,
        byte[] codewords
    )
    {
        var bitIndex = 0;
        var totalBits = codewords.Length * 8;
        var upward = true;

        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            for (var step = 0; step < size; step++)
            {
                var y = upward ? size - 1 - step : step;
                for (var dx = 0; dx < 2; dx++)
                {
                    var x = right - dx;
                    if (isFunction[y, x])
                    {
                        continue;
                    }

                    var bit =
                        bitIndex < totalBits
                        && ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) == 1;
                    bitIndex++;

                    // Mask 0: (y + x) % 2 == 0
                    modules[y, x] = ((y + x) % 2 == 0) ? !bit : bit;
                }
            }

            upward = !upward;
        }
    }

    private static void WriteFormatBits(bool[,] modules, int size, int formatBits)
    {
        for (var i = 0; i < 6; i++)
        {
            modules[i, 8] = ((formatBits >> i) & 1) == 1;
            modules[8, size - 1 - i] = ((formatBits >> i) & 1) == 1;
        }

        modules[7, 8] = ((formatBits >> 6) & 1) == 1;
        modules[8, 8] = ((formatBits >> 7) & 1) == 1;
        modules[8, 7] = ((formatBits >> 8) & 1) == 1;

        for (var i = 9; i < 15; i++)
        {
            var bit = ((formatBits >> i) & 1) == 1;
            modules[8, 14 - i] = bit;
            modules[size - 15 + i, 8] = bit;
        }

        modules[8, size - 8] = ((formatBits >> 7) & 1) == 1;
        modules[8, size - 7] = ((formatBits >> 6) & 1) == 1;
    }

    private static byte[] BuildReedSolomonGenerator(int degree)
    {
        var poly = new byte[degree];
        poly[degree - 1] = 1;
        byte root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                poly[j] = GfMultiply(poly[j], root);
                if (j + 1 < degree)
                {
                    poly[j] ^= poly[j + 1];
                }
            }

            root = GfMultiply(root, 0x02);
        }

        return poly;
    }

    private static byte[] ComputeReedSolomonRemainder(byte[] data, byte[] generator)
    {
        var result = new byte[generator.Length];
        foreach (var b in data)
        {
            var factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (var i = 0; i < generator.Length; i++)
            {
                result[i] ^= GfMultiply(generator[i], factor);
            }
        }

        return result;
    }

    private static byte GfMultiply(byte x, byte y)
    {
        byte z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (byte)((z << 1) ^ ((z >> 7) * 0x11D));
            z ^= (byte)(((y >> i) & 1) * x);
        }

        return z;
    }

    private static void AppendBits(List<bool> bits, int value, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            bits.Add(((value >> i) & 1) == 1);
        }
    }

    private static string RenderSvg(bool[,] modules, int size, int quietZone)
    {
        var total = size + (quietZone * 2);
        var sb = new StringBuilder(total * total * 4);
        _ = sb.Append(
            string.Create(
                CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {total} {total}\" shape-rendering=\"crispEdges\" role=\"img\" aria-label=\"Shortlink QR Code\">"
            )
        );
        _ = sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
        _ = sb.Append("<path fill=\"#111827\" d=\"");
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (modules[y, x])
                {
                    _ = sb.Append(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"M{x + quietZone},{y + quietZone}h1v1h-1z"
                        )
                    );
                }
            }
        }

        _ = sb.Append("\"/></svg>");
        return sb.ToString();
    }
}
