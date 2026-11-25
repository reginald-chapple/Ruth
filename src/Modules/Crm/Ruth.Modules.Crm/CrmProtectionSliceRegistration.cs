using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Slices.Assessments;
using Ruth.Modules.Crm.Slices.BackgroundChecks;
using Ruth.Modules.Crm.Slices.Badges;
using Ruth.Modules.Crm.Slices.Signals;
using Ruth.Modules.Crm.Slices.Verification;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Crm;

internal static class CrmProtectionSliceRegistration
{
    public static void RegisterSignalsAndBadgesHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateSignalTypeCommand,
            SignalTypeResponse,
            CreateSignalTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateSignalTypeCommand,
            SignalTypeResponse,
            UpdateSignalTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSignalTypesQuery,
            IReadOnlyList<SignalTypeResponse>,
            GetSignalTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            AddPersonSignalCommand,
            PersonSignalResponse,
            AddPersonSignalCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemovePersonSignalCommand,
            bool,
            RemovePersonSignalCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonSignalsQuery,
            IReadOnlyList<PersonSignalResponse>,
            GetPersonSignalsQueryHandler
        >();

        _ = services.AddCommandHandler<
            CreateBadgeDefinitionCommand,
            BadgeDefinitionResponse,
            CreateBadgeDefinitionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetBadgeDefinitionsQuery,
            IReadOnlyList<BadgeDefinitionResponse>,
            GetBadgeDefinitionsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEvaluatedPersonBadgesQuery,
            IReadOnlyList<EvaluatedPersonBadgeResponse>,
            GetEvaluatedPersonBadgesQueryHandler
        >();
    }

    public static void RegisterAssessmentsAndChecksHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateAssessmentTypeCommand,
            AssessmentTypeResponse,
            CreateAssessmentTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAssessmentTypesQuery,
            IReadOnlyList<AssessmentTypeResponse>,
            GetAssessmentTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            RequestAssessmentCommand,
            AssessmentResponse,
            RequestAssessmentCommandHandler
        >();
        _ = services.AddCommandHandler<
            CompleteAssessmentCommand,
            AssessmentResponse,
            CompleteAssessmentCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonAssessmentsQuery,
            IReadOnlyList<AssessmentResponse>,
            GetPersonAssessmentsQueryHandler
        >();

        _ = services.AddQueryHandler<
            GetBackgroundCheckPackagesQuery,
            IReadOnlyList<BackgroundCheckPackageResponse>,
            GetBackgroundCheckPackagesQueryHandler
        >();
        _ = services.AddCommandHandler<
            RequestBackgroundCheckCommand,
            BackgroundCheckResponse,
            RequestBackgroundCheckCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessBackgroundCheckWebhookCommand,
            BackgroundCheckResponse,
            ProcessBackgroundCheckWebhookCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonBackgroundChecksQuery,
            IReadOnlyList<BackgroundCheckResponse>,
            GetPersonBackgroundChecksQueryHandler
        >();
    }

    public static void RegisterVerificationAndViewedHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            IssueIdentityVerificationCommand,
            IdentityVerificationIssuedResponse,
            IssueIdentityVerificationCommandHandler
        >();
        _ = services.AddCommandHandler<
            VerifyIdentityCodeCommand,
            IdentityVerificationResultResponse,
            VerifyIdentityCodeCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordPersonViewedCommand,
            PersonViewedResponse,
            RecordPersonViewedCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonViewedHistoryQuery,
            IReadOnlyList<PersonViewedResponse>,
            GetPersonViewedHistoryQueryHandler
        >();
    }
}
