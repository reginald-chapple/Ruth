using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Lists registered PersonalDevice push tokens.</summary>
/// <param name="PersonId">Optional CRM person filter.</param>
/// <param name="ActiveOnly">Whether to return only active opted-in devices.</param>
public sealed record GetPersonalDevicesQuery(Guid? PersonId = null, bool ActiveOnly = false)
    : IQuery<IReadOnlyList<PersonalDeviceResponse>>;
