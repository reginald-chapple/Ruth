using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Domain.CadenceAlerts;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal static class CadenceAlertMapper
{
    public static TransactionCadenceAlertTypeResponse ToTypeResponse(
        TransactionCadenceAlertType entity
    )
    {
        return new TransactionCadenceAlertTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.AlertKind,
            entity.MinHistoricalSamples,
            entity.MultiplierThreshold,
            entity.StandardDeviationSensitivity,
            entity.MinimumAmountThreshold,
            entity.CooldownDays,
            entity.AccountId?.Value,
            entity.IsActive
        );
    }

    public static TransactionCadenceAlertResponse ToAlertResponse(
        TransactionCadenceAlert entity,
        string alertTypeName
    )
    {
        return new TransactionCadenceAlertResponse(
            entity.Id.Value,
            entity.AlertTypeId.Value,
            alertTypeName,
            entity.AlertKind,
            entity.PersonId,
            entity.Status,
            entity.BaselineMeanAmount,
            entity.BaselineIntervalDays,
            entity.ObservedValue,
            entity.Summary,
            entity.ResolutionNotes,
            entity.DetectedOnUtc
        );
    }
}
