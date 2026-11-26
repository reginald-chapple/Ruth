using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Fee group configured on a RegistrationTemplate.</summary>
public sealed class RegistrationTemplateFee : Entity<RegistrationTemplateFeeId>
{
    private readonly List<RegistrationTemplateFeeItem> _items = [];

    private RegistrationTemplateFee() { }

    private RegistrationTemplateFee(RegistrationTemplateFeeId id, RegistrationTemplateId templateId)
        : base(id)
    {
        TemplateId = templateId;
    }

    public RegistrationTemplateId TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public RegistrationFeeType FeeType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool AllowDiscount { get; private set; } = true;
    public int DisplayOrder { get; private set; }
    public IReadOnlyCollection<RegistrationTemplateFeeItem> Items => _items.AsReadOnly();

    internal static Result<RegistrationTemplateFee> Create(
        RegistrationTemplateId templateId,
        RegistrationFeeSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 160
            || spec.Items is not { Count: > 0 }
        )
        {
            return EventsRegistrationErrors.TemplateInvalid;
        }

        var fee = new RegistrationTemplateFee(RegistrationTemplateFeeId.Create(), templateId)
        {
            Name = spec.Name.Trim(),
            FeeType = spec.FeeType,
            IsRequired = spec.IsRequired,
            AllowDiscount = spec.AllowDiscount,
            DisplayOrder = spec.DisplayOrder,
        };

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var itemSpec in spec.Items)
        {
            var itemRes = RegistrationTemplateFeeItem.Create(fee.Id, itemSpec);
            if (itemRes.IsFailure || !codes.Add(itemRes.Value.Code))
            {
                return EventsRegistrationErrors.TemplateInvalid;
            }

            fee._items.Add(itemRes.Value);
        }

        return fee;
    }
}
