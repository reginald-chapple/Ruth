using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain.Labels;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal static class CheckInLabelMapper
{
    public static CheckInLabelResponse ToResponse(CheckInLabel label)
    {
        return new CheckInLabelResponse(
            label.Id.Value,
            label.Name,
            label.Key,
            label.Description,
            label.LabelKind,
            label.ConfigurationId,
            label.AreaId,
            label.Layout.ToDto(),
            label.CustomZplOverride,
            label.PrintOrder,
            label.Copies,
            label.IsActive,
            label.IsSystem,
            label.CreatedOnUtc,
            label.ModifiedOnUtc
        );
    }

    public static CheckInLabelSpec ToSpec(CreateCheckInLabelCommand cmd)
    {
        return new CheckInLabelSpec(
            cmd.Name,
            cmd.Key,
            cmd.LabelKind,
            cmd.Layout,
            cmd.Description,
            cmd.ConfigurationId,
            cmd.AreaId,
            cmd.CustomZplOverride,
            cmd.PrintOrder,
            cmd.Copies,
            cmd.IsActive,
            IsSystem: false
        );
    }

    public static CheckInLabelSpec ToSpec(UpdateCheckInLabelCommand cmd, bool isSystem)
    {
        return new CheckInLabelSpec(
            cmd.Name,
            cmd.Key,
            cmd.LabelKind,
            cmd.Layout,
            cmd.Description,
            cmd.ConfigurationId,
            cmd.AreaId,
            cmd.CustomZplOverride,
            cmd.PrintOrder,
            cmd.Copies,
            cmd.IsActive,
            isSystem
        );
    }
}
