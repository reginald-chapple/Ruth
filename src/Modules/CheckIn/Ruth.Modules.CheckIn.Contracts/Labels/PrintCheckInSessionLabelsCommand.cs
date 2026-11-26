using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Compiles and dispatches participant badges and guardian pickup receipts for a check-in session.</summary>
/// <param name="SessionId">Target check-in session identifier.</param>
/// <param name="KioskDeviceId">Optional kiosk Core Device identifier overriding the session's device.</param>
/// <param name="DirectPrinterEndpointOverride">Optional explicit "host:port" TCP target for server dispatch.</param>
/// <param name="ForceClientPrint">When true, returns compiled ZPL to the client for WebUSB/local print instead of server TCP.</param>
public sealed record PrintCheckInSessionLabelsCommand(
    Guid SessionId,
    Guid? KioskDeviceId = null,
    string? DirectPrinterEndpointOverride = null,
    bool ForceClientPrint = false
) : ICommand<PrintCheckInSessionLabelsResponse>;
