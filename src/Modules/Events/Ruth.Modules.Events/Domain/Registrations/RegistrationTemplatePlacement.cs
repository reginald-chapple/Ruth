using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Group placement target configuration on a RegistrationTemplate.</summary>
public sealed class RegistrationTemplatePlacement : Entity<RegistrationTemplatePlacementId>
{
    private RegistrationTemplatePlacement() { }

    private RegistrationTemplatePlacement(
        RegistrationTemplatePlacementId id,
        RegistrationTemplateId templateId
    )
        : base(id)
    {
        TemplateId = templateId;
    }

    public RegistrationTemplateId TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid GroupTypeId { get; private set; }
    public Guid? DefaultTargetGroupId { get; private set; }
    public Guid? DefaultGroupRoleId { get; private set; }
    public bool AutoPlaceOnConfirmation { get; private set; }

    internal static Result<RegistrationTemplatePlacement> Create(
        RegistrationTemplateId templateId,
        RegistrationGroupPlacementSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var invalid =
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 160
            || spec.GroupTypeId == Guid.Empty;

        return invalid
            ? EventsRegistrationErrors.TemplateInvalid
            : new RegistrationTemplatePlacement(
                RegistrationTemplatePlacementId.Create(),
                templateId
            )
            {
                Name = spec.Name.Trim(),
                GroupTypeId = spec.GroupTypeId,
                DefaultTargetGroupId =
                    spec.DefaultTargetGroupId is { } g && g != Guid.Empty ? g : null,
                DefaultGroupRoleId = spec.DefaultGroupRoleId is { } r && r != Guid.Empty ? r : null,
                AutoPlaceOnConfirmation = spec.AutoPlaceOnConfirmation,
            };
    }
}
