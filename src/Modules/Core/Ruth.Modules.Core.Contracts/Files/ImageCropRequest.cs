namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Image crop rectangle and optional target dimensions for avatar or image processing.</summary>
public sealed record ImageCropRequest(
    int X,
    int Y,
    int Width,
    int Height,
    int? TargetWidth = null,
    int? TargetHeight = null,
    string OutputFormat = "png"
);
