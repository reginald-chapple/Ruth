using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.SharedKernel.Results;
using StbImageSharp;
using StbImageWriteSharp;

namespace Ruth.Modules.Core.Infrastructure.Imaging;

internal sealed class StbImageProcessor : IImageProcessor
{
    public Result<(int Width, int Height)> TryReadDimensions(byte[] imageBytes)
    {
        if (imageBytes.Length == 0)
        {
            return CoreDocumentErrors.BinaryFileNotAnImage;
        }

        try
        {
            using var ms = new MemoryStream(imageBytes, writable: false);
            var info = ImageInfo.FromStream(ms);
            return info.HasValue
                ? Result.Success((info.Value.Width, info.Value.Height))
                : CoreDocumentErrors.BinaryFileNotAnImage;
        }
        catch (InvalidOperationException)
        {
            return CoreDocumentErrors.BinaryFileNotAnImage;
        }
    }

    public Result<ProcessedImageResult> ResizeToBoundsIfNeeded(
        byte[] imageBytes,
        string mimeType,
        int? maxWidth,
        int? maxHeight
    )
    {
        var decoded = DecodeRgba(imageBytes);
        if (decoded is null)
        {
            return CoreDocumentErrors.BinaryFileNotAnImage;
        }

        var (targetW, targetH) = ComputeConstrainedSize(
            decoded.Width,
            decoded.Height,
            maxWidth,
            maxHeight
        );
        if (targetW == decoded.Width && targetH == decoded.Height)
        {
            return Result.Success(
                new ProcessedImageResult(imageBytes, mimeType, decoded.Width, decoded.Height)
            );
        }

        var resized = ResizeBilinear(decoded.Data, decoded.Width, decoded.Height, targetW, targetH);
        var isJpeg = string.Equals(mimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase);
        var (Bytes, MimeType) = EncodeImage(resized, targetW, targetH, isJpeg ? "jpg" : "png");
        return Result.Success(new ProcessedImageResult(Bytes, MimeType, targetW, targetH));
    }

    public Result<ProcessedImageResult> CropAndResize(byte[] imageBytes, ImageCropRequest request)
    {
        var decoded = DecodeRgba(imageBytes);
        if (decoded is null)
        {
            return CoreDocumentErrors.BinaryFileNotAnImage;
        }

        if (
            request.X < 0
            || request.Y < 0
            || request.Width <= 0
            || request.Height <= 0
            || request.X + request.Width > decoded.Width
            || request.Y + request.Height > decoded.Height
        )
        {
            return CoreDocumentErrors.ImageCropBoundsInvalid;
        }

        var cropped = CropRgba(
            decoded.Data,
            decoded.Width,
            request.X,
            request.Y,
            request.Width,
            request.Height
        );
        var finalW = request.TargetWidth is > 0 ? request.TargetWidth.Value : request.Width;
        var finalH = request.TargetHeight is > 0 ? request.TargetHeight.Value : request.Height;

        var pixels =
            finalW == request.Width && finalH == request.Height
                ? cropped
                : ResizeBilinear(cropped, request.Width, request.Height, finalW, finalH);

        var (Bytes, MimeType) = EncodeImage(pixels, finalW, finalH, request.OutputFormat);
        return Result.Success(new ProcessedImageResult(Bytes, MimeType, finalW, finalH));
    }

    private static ImageResult? DecodeRgba(byte[] imageBytes)
    {
        try
        {
            return ImageResult.FromMemory(
                imageBytes,
                StbImageSharp.ColorComponents.RedGreenBlueAlpha
            );
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static (int Width, int Height) ComputeConstrainedSize(
        int width,
        int height,
        int? maxWidth,
        int? maxHeight
    )
    {
        var scaleW =
            maxWidth is > 0 && width > maxWidth.Value ? (double)maxWidth.Value / width : 1.0;
        var scaleH =
            maxHeight is > 0 && height > maxHeight.Value ? (double)maxHeight.Value / height : 1.0;
        var scale = Math.Min(scaleW, scaleH);
        if (scale >= 1.0)
        {
            return (width, height);
        }

        return (
            Math.Max(1, (int)Math.Round(width * scale, MidpointRounding.ToEven)),
            Math.Max(1, (int)Math.Round(height * scale, MidpointRounding.ToEven))
        );
    }

    private static byte[] CropRgba(byte[] src, int srcWidth, int x, int y, int width, int height)
    {
        var dst = new byte[width * height * 4];
        var rowBytes = width * 4;
        for (var row = 0; row < height; row++)
        {
            var srcOffset = (((y + row) * srcWidth) + x) * 4;
            var dstOffset = row * rowBytes;
            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, rowBytes);
        }

        return dst;
    }

    private static byte[] ResizeBilinear(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        var dst = new byte[dstW * dstH * 4];
        var xRatio = srcW > 1 && dstW > 1 ? (double)(srcW - 1) / (dstW - 1) : 0;
        var yRatio = srcH > 1 && dstH > 1 ? (double)(srcH - 1) / (dstH - 1) : 0;

        for (var dy = 0; dy < dstH; dy++)
        {
            var sy = dy * yRatio;
            var y0 = (int)sy;
            var y1 = Math.Min(y0 + 1, srcH - 1);
            var wy = sy - y0;

            for (var dx = 0; dx < dstW; dx++)
            {
                var sx = dx * xRatio;
                var x0 = (int)sx;
                var x1 = Math.Min(x0 + 1, srcW - 1);
                var wx = sx - x0;
                InterpolatePixel(src, srcW, dst, dstW, dx, dy, x0, y0, x1, y1, wx, wy);
            }
        }

        return dst;
    }

    private static void InterpolatePixel(
        byte[] src,
        int srcW,
        byte[] dst,
        int dstW,
        int dx,
        int dy,
        int x0,
        int y0,
        int x1,
        int y1,
        double wx,
        double wy
    )
    {
        var dstIdx = ((dy * dstW) + dx) * 4;
        var i00 = ((y0 * srcW) + x0) * 4;
        var i10 = ((y0 * srcW) + x1) * 4;
        var i01 = ((y1 * srcW) + x0) * 4;
        var i11 = ((y1 * srcW) + x1) * 4;

        for (var c = 0; c < 4; c++)
        {
            var top = (src[i00 + c] * (1.0 - wx)) + (src[i10 + c] * wx);
            var bottom = (src[i01 + c] * (1.0 - wx)) + (src[i11 + c] * wx);
            var value = (top * (1.0 - wy)) + (bottom * wy);
            dst[dstIdx + c] = (byte)
                Math.Clamp((int)Math.Round(value, MidpointRounding.ToEven), 0, 255);
        }
    }

    private static (byte[] Bytes, string MimeType) EncodeImage(
        byte[] rgbaPixels,
        int width,
        int height,
        string outputFormat
    )
    {
        var writer = new ImageWriter();
        using var ms = new MemoryStream();
        if (
            string.Equals(outputFormat, "jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outputFormat, "jpeg", StringComparison.OrdinalIgnoreCase)
        )
        {
            writer.WriteJpg(
                rgbaPixels,
                width,
                height,
                StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
                ms,
                quality: 90
            );
            return (ms.ToArray(), "image/jpeg");
        }

        writer.WritePng(
            rgbaPixels,
            width,
            height,
            StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
            ms
        );
        return (ms.ToArray(), "image/png");
    }
}
