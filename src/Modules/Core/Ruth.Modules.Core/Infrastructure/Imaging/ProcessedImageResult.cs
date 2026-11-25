namespace Ruth.Modules.Core.Infrastructure.Imaging;

internal sealed record ProcessedImageResult(byte[] Bytes, string MimeType, int Width, int Height);
