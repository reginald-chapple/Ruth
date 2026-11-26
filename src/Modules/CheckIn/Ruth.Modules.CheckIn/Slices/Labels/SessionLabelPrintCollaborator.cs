using System.Globalization;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class SessionLabelPrintCollaborator(
    IQueryDispatcher queryDispatcher,
    IZplPrinterDispatcher zplDispatcher
)
{
    public async Task<(
        bool PrintFromServer,
        PrinterEndpointTarget? Endpoint
    )> ResolvePrinterRoutingAsync(
        PrintCheckInSessionLabelsCommand command,
        AttendanceCheckInSession session,
        CancellationToken cancellationToken
    )
    {
        if (command.ForceClientPrint)
        {
            return (PrintFromServer: false, Endpoint: null);
        }

        if (!string.IsNullOrWhiteSpace(command.DirectPrinterEndpointOverride))
        {
            return (
                PrintFromServer: true,
                new PrinterEndpointTarget(
                    "Direct TCP Override",
                    command.DirectPrinterEndpointOverride.Trim()
                )
            );
        }

        var kioskId = command.KioskDeviceId ?? session.KioskDeviceId;
        return !kioskId.HasValue || kioskId.Value == Guid.Empty
            ? (PrintFromServer: false, Endpoint: null)
            : await ResolveDevicePrinterEndpointAsync(kioskId.Value, cancellationToken);
    }

    private async Task<(
        bool PrintFromServer,
        PrinterEndpointTarget? Endpoint
    )> ResolveDevicePrinterEndpointAsync(Guid kioskDeviceId, CancellationToken cancellationToken)
    {
        var kioskResult = await queryDispatcher.QueryAsync<GetDeviceByIdQuery, DeviceResponse>(
            new GetDeviceByIdQuery(kioskDeviceId),
            cancellationToken
        );
        if (kioskResult.IsFailure || kioskResult.Value.PrintFrom != DevicePrintFrom.Server)
        {
            return (PrintFromServer: false, Endpoint: null);
        }

        var printerDeviceId = kioskResult.Value.PrinterDeviceId;
        if (!printerDeviceId.HasValue || printerDeviceId.Value == Guid.Empty)
        {
            return (
                PrintFromServer: true,
                Endpoint: new PrinterEndpointTarget(
                    kioskResult.Value.Name,
                    kioskResult.Value.IpAddress ?? string.Empty,
                    kioskResult.Value.Id
                )
            );
        }

        var printerResult = await queryDispatcher.QueryAsync<GetDeviceByIdQuery, DeviceResponse>(
            new GetDeviceByIdQuery(printerDeviceId.Value),
            cancellationToken
        );
        return printerResult.IsFailure
            ? (PrintFromServer: false, Endpoint: null)
            : (
                PrintFromServer: true,
                Endpoint: new PrinterEndpointTarget(
                    printerResult.Value.Name,
                    printerResult.Value.IpAddress ?? string.Empty,
                    printerResult.Value.Id
                )
            );
    }

    public async Task<CheckInLabelMergeContextDto> BuildParticipantMergeContextAsync(
        AttendanceCheckInSession session,
        CheckInAttendanceEntry entry,
        CancellationToken cancellationToken
    )
    {
        var inv = CultureInfo.InvariantCulture;
        var personResult = await queryDispatcher.QueryAsync<
            GetPersonByIdQuery,
            PersonDetailResponse
        >(new GetPersonByIdQuery(entry.PersonId), cancellationToken);

        var nickName = personResult.IsSuccess
            ? personResult.Value.NickName
            : ExtractFirstToken(entry.PersonFullName);
        var lastName = personResult.IsSuccess
            ? personResult.Value.LastName
            : ExtractLastToken(entry.PersonFullName);

        string? tier = null;
        string? allergy = null;
        string[] badgeIcons = [];
        if (personResult.IsSuccess)
        {
            _ = personResult.Value.Attributes.TryGetValue("MembershipTier", out tier);
            _ = personResult.Value.Attributes.TryGetValue("Allergies", out allergy);
            if (
                personResult.Value.Attributes.TryGetValue("Skills", out var skills)
                && !string.IsNullOrWhiteSpace(skills)
            )
            {
                badgeIcons = skills.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
            }
        }

        var signalsResult = await queryDispatcher.QueryAsync<
            GetPersonSignalsQuery,
            IReadOnlyList<PersonSignalResponse>
        >(new GetPersonSignalsQuery(entry.PersonId), cancellationToken);
        var signalSummary =
            signalsResult.IsSuccess && signalsResult.Value.Count > 0
                ? string.Join(", ", signalsResult.Value.Select(static s => s.SignalTypeName))
                : null;

        return new CheckInLabelMergeContextDto(
            entry.PersonFullName,
            nickName,
            lastName,
            entry.SecurityPickupCode ?? session.SecurityPickupCode,
            entry.AreaName,
            entry.LocationName,
            session.ConfigurationName,
            session.ServiceDate.ToString("yyyy-MM-dd", inv),
            entry.CheckedInOnUtc.ToString("HH:mm", inv) + " UTC",
            allergy,
            signalSummary,
            entry.AttendanceNote,
            GuardianReceiptSummary: null,
            tier,
            badgeIcons,
            session.MobileExpressToken ?? entry.SecurityPickupCode ?? entry.Id.Value.ToString("N")
        );
    }

    public Task<Result<ZplPrintDispatchReceipt>> DispatchToServerPrinterAsync(
        PrinterEndpointTarget endpoint,
        string zpl,
        CancellationToken cancellationToken
    )
    {
        return zplDispatcher.DispatchRawZplAsync(endpoint, zpl, cancellationToken);
    }

    private static string ExtractFirstToken(string fullName)
    {
        var idx = fullName.IndexOf(' ', StringComparison.Ordinal);
        return idx > 0 ? fullName[..idx] : fullName;
    }

    private static string ExtractLastToken(string fullName)
    {
        var idx = fullName.LastIndexOf(' ');
        return idx > 0 && idx < fullName.Length - 1 ? fullName[(idx + 1)..] : string.Empty;
    }
}
