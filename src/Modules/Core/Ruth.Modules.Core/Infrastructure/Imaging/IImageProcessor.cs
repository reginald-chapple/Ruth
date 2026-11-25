using Ruth.Modules.Core.Contracts.Files;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Infrastructure.Imaging;

internal interface IImageProcessor
{
    Result<(int Width, int Height)> TryReadDimensions(byte[] imageBytes);

    Result<ProcessedImageResult> ResizeToBoundsIfNeeded(
        byte[] imageBytes,
        string mimeType,
        int? maxWidth,
        int? maxHeight
    );

    Result<ProcessedImageResult> CropAndResize(byte[] imageBytes, ImageCropRequest request);
}
