using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Shell layout template used by screens inside a <see cref="MobileApplication" />.</summary>
public sealed class MobileLayout : Entity<MobileLayoutId>
{
    private MobileLayout() { }

    private MobileLayout(
        MobileLayoutId id,
        MobileApplicationId mobileApplicationId,
        string name,
        string layoutKey,
        string shellMarkup,
        int order
    )
        : base(id)
    {
        MobileApplicationId = mobileApplicationId;
        Name = name;
        LayoutKey = layoutKey;
        ShellMarkup = shellMarkup;
        Order = order;
    }

    public MobileApplicationId MobileApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string LayoutKey { get; private set; } = string.Empty;
    public string ShellMarkup { get; private set; } = string.Empty;
    public int Order { get; private set; }

    public static Result<MobileLayout> Create(
        MobileApplicationId mobileApplicationId,
        string name,
        string layoutKey,
        string shellMarkup,
        int order = 0
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(layoutKey);
        return
            mobileApplicationId.IsEmpty
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(normalizedKey)
            ? Result.Failure<MobileLayout>(
                CmsErrors.Validation(
                    "MobileLayout.Invalid",
                    "MobileApplicationId, Name, and LayoutKey are required."
                )
            )
            : Result.Success(
                new MobileLayout(
                    MobileLayoutId.Create(),
                    mobileApplicationId,
                    name.Trim(),
                    normalizedKey,
                    shellMarkup ?? string.Empty,
                    order
                )
            );
    }
}
