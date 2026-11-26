using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Dynamic form field definition within a RegistrationTemplateForm.</summary>
public sealed class RegistrationTemplateFormField : Entity<RegistrationTemplateFormFieldId>
{
    private List<string> _selectOptions = [];

    private RegistrationTemplateFormField() { }

    private RegistrationTemplateFormField(
        RegistrationTemplateFormFieldId id,
        RegistrationTemplateFormId formId
    )
        : base(id)
    {
        FormId = formId;
    }

    public RegistrationTemplateFormId FormId { get; private set; }
    public string FieldKey { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public RegistrationFieldKind FieldKind { get; private set; }
    public bool IsRequired { get; private set; }
    public IReadOnlyList<string> SelectOptions
    {
        get => _selectOptions;
        private set => _selectOptions = [.. value];
    }

    public string? ShowWhenFieldKey { get; private set; }
    public string? ShowWhenEqualsValue { get; private set; }
    public int DisplayOrder { get; private set; }

    internal static Result<RegistrationTemplateFormField> Create(
        RegistrationTemplateFormId formId,
        RegistrationFormFieldSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);

        var cleanedOptions = spec.SelectOptions?.Where(static o => !string.IsNullOrWhiteSpace(o))
            .Select(static o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];

        var requiresOptions =
            spec.FieldKind
            is RegistrationFieldKind.SingleSelect
                or RegistrationFieldKind.MultiSelect;
        var invalid =
            string.IsNullOrWhiteSpace(spec.FieldKey)
            || spec.FieldKey.Trim().Length > 80
            || string.IsNullOrWhiteSpace(spec.Label)
            || spec.Label.Trim().Length > 160
            || (requiresOptions && cleanedOptions.Count == 0);

        return invalid
            ? EventsRegistrationErrors.TemplateInvalid
            : new RegistrationTemplateFormField(RegistrationTemplateFormFieldId.Create(), formId)
            {
                FieldKey = spec.FieldKey.Trim(),
                Label = spec.Label.Trim(),
                FieldKind = spec.FieldKind,
                IsRequired = spec.IsRequired,
                SelectOptions = cleanedOptions,
                ShowWhenFieldKey = string.IsNullOrWhiteSpace(spec.ShowWhenFieldKey)
                    ? null
                    : spec.ShowWhenFieldKey.Trim(),
                ShowWhenEqualsValue = string.IsNullOrWhiteSpace(spec.ShowWhenEqualsValue)
                    ? null
                    : spec.ShowWhenEqualsValue.Trim(),
                DisplayOrder = spec.DisplayOrder,
            };
    }
}
