using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Validated timed drip step value model within a <see cref="CommunicationFlow" />.</summary>
public sealed partial record CommunicationFlowStep(
    int Order,
    string StepKey,
    string Name,
    CommunicationChannel Channel,
    int DelayMinutes,
    string? SubjectTemplate,
    string? BodyTemplate,
    Guid? CommunicationTemplateId,
    bool SendOnlyIfPreviousUnopened
)
{
    private const int MaxSteps = 25;

    /// <summary>Validates and normalizes a collection of <see cref="CommunicationFlowStepDto" /> definitions.</summary>
    public static Result<IReadOnlyList<CommunicationFlowStep>> ValidateAndNormalize(
        IReadOnlyList<CommunicationFlowStepDto>? inputs
    )
    {
        if (inputs is null || inputs.Count is 0 or > MaxSteps)
        {
            return Result.Failure<IReadOnlyList<CommunicationFlowStep>>(
                CommunicationFlowErrors.FlowStepsInvalid
            );
        }

        var list = new List<CommunicationFlowStep>(inputs.Count);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in inputs.OrderBy(static s => s.Order))
        {
            var stepResult = ValidateSingle(dto, list.Count);
            if (stepResult.IsFailure || !seenKeys.Add(stepResult.Value.StepKey))
            {
                return Result.Failure<IReadOnlyList<CommunicationFlowStep>>(
                    CommunicationFlowErrors.FlowStepsInvalid
                );
            }

            list.Add(stepResult.Value);
        }

        return Result.Success<IReadOnlyList<CommunicationFlowStep>>(list);
    }

    public CommunicationFlowStepDto ToDto()
    {
        return new CommunicationFlowStepDto(
            Order,
            StepKey,
            Name,
            Channel,
            DelayMinutes,
            SubjectTemplate,
            BodyTemplate,
            CommunicationTemplateId,
            SendOnlyIfPreviousUnopened
        );
    }

    private static Result<CommunicationFlowStep> ValidateSingle(
        CommunicationFlowStepDto dto,
        int normalizedOrder
    )
    {
        var templateId = ResolveTemplateId(dto.CommunicationTemplateId);
        return !HasValidStepEnvelope(dto) || !ValidateDirectContent(dto, templateId.HasValue)
            ? Result.Failure<CommunicationFlowStep>(CommunicationFlowErrors.FlowStepsInvalid)
            : new CommunicationFlowStep(
                normalizedOrder,
                dto.StepKey.Trim(),
                dto.Name.Trim(),
                dto.Channel,
                dto.DelayMinutes,
                Clean(dto.SubjectTemplate),
                Clean(dto.BodyTemplate),
                templateId,
                dto.SendOnlyIfPreviousUnopened && normalizedOrder > 0
            );
    }

    private static Guid? ResolveTemplateId(Guid? rawId)
    {
        return rawId.HasValue && rawId.Value != Guid.Empty ? rawId : null;
    }

    private static bool HasValidStepEnvelope(CommunicationFlowStepDto dto)
    {
        var key = dto.StepKey?.Trim() ?? string.Empty;
        var name = dto.Name?.Trim() ?? string.Empty;
        return StepSlugRegex.IsMatch(key)
            && name.Length is > 0 and <= 120
            && dto.DelayMinutes is >= 0 and <= 525_600;
    }

    private static bool ValidateDirectContent(CommunicationFlowStepDto dto, bool hasTemplateRef)
    {
        return hasTemplateRef
            || dto.Channel switch
            {
                CommunicationChannel.Email => !string.IsNullOrWhiteSpace(dto.SubjectTemplate)
                    && !string.IsNullOrWhiteSpace(dto.BodyTemplate),
                CommunicationChannel.Sms or CommunicationChannel.Push or _ =>
                    !string.IsNullOrWhiteSpace(dto.BodyTemplate),
            };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{1,79}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex StepSlugRegex { get; }
}
