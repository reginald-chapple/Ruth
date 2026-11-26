using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Telemetry channel grouping interaction components (Websites, Comms, Mobile, Signage).</summary>
public sealed class InteractionChannel
    : AggregateRoot<InteractionChannelId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private InteractionChannel() { }

    private InteractionChannel(InteractionChannelId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public InteractionChannelType ChannelType { get; private set; }
    public int? RetentionDurationDays { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<InteractionChannel> Create(
        string name,
        string key,
        InteractionChannelType channelType = InteractionChannelType.Website,
        int? retentionDurationDays = 365
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(key);
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedKey)
                ? Result.Failure<InteractionChannel>(
                    CmsErrors.Validation(
                        "InteractionChannel.Invalid",
                        "Interaction channel name and key are required."
                    )
                )
            : retentionDurationDays is <= 0
                ? Result.Failure<InteractionChannel>(
                    CmsErrors.Validation(
                        "InteractionChannel.RetentionInvalid",
                        "Retention duration must be positive when provided."
                    )
                )
            : Result.Success(
                new InteractionChannel(InteractionChannelId.Create())
                {
                    Name = name.Trim(),
                    Key = normalizedKey,
                    ChannelType = channelType,
                    RetentionDurationDays = retentionDurationDays,
                    IsActive = true,
                }
            );
    }
}
