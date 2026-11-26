using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Dynamic form section within a RegistrationTemplate.</summary>
public sealed class RegistrationTemplateForm : Entity<RegistrationTemplateFormId>
{
    private readonly List<RegistrationTemplateFormField> _fields = [];

    private RegistrationTemplateForm() { }

    private RegistrationTemplateForm(
        RegistrationTemplateFormId id,
        RegistrationTemplateId templateId
    )
        : base(id)
    {
        TemplateId = templateId;
    }

    public RegistrationTemplateId TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public IReadOnlyCollection<RegistrationTemplateFormField> Fields => _fields.AsReadOnly();

    internal static Result<RegistrationTemplateForm> Create(
        RegistrationTemplateId templateId,
        RegistrationFormSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Name) || spec.Name.Trim().Length > 160)
        {
            return EventsRegistrationErrors.TemplateInvalid;
        }

        var form = new RegistrationTemplateForm(RegistrationTemplateFormId.Create(), templateId)
        {
            Name = spec.Name.Trim(),
            DisplayOrder = spec.DisplayOrder,
        };

        if (spec.Fields is { Count: > 0 })
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fieldSpec in spec.Fields)
            {
                var fieldRes = RegistrationTemplateFormField.Create(form.Id, fieldSpec);
                if (fieldRes.IsFailure || !keys.Add(fieldRes.Value.FieldKey))
                {
                    return EventsRegistrationErrors.TemplateInvalid;
                }

                form._fields.Add(fieldRes.Value);
            }
        }

        return form;
    }
}
