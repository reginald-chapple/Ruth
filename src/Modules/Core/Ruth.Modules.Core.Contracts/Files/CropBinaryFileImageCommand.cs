using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Crops and optionally resizes an image binary file into an avatar or edited image.</summary>
public sealed record CropBinaryFileImageCommand(
    Guid BinaryFileId,
    ImageCropRequest Crop,
    bool CreateCopy = false
) : ICommand<BinaryFileResponse>;
