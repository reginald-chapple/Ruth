using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Slices.Duplicates;
using Ruth.Modules.Crm.Slices.Households;
using Ruth.Modules.Crm.Slices.People;
using Ruth.Modules.Crm.Slices.Relationships;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Crm;

internal static class CrmCoreSliceRegistration
{
    public static void RegisterPeopleHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreatePersonCommand,
            PersonDetailResponse,
            CreatePersonCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdatePersonCommand,
            PersonDetailResponse,
            UpdatePersonCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpsertPhoneNumberCommand,
            PersonDetailResponse,
            UpsertPhoneNumberCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemovePhoneNumberCommand,
            PersonDetailResponse,
            RemovePhoneNumberCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddPersonPreviousNameCommand,
            PersonDetailResponse,
            AddPersonPreviousNameCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddPersonSearchKeyCommand,
            PersonDetailResponse,
            AddPersonSearchKeyCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemovePersonSearchKeyCommand,
            PersonDetailResponse,
            RemovePersonSearchKeyCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdatePersonAttributesCommand,
            PersonDetailResponse,
            UpdatePersonAttributesCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonByIdQuery,
            PersonDetailResponse,
            GetPersonByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>,
            SearchPeopleQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetMetaNameInsightsQuery,
            MetaNameInsightsResponse,
            GetMetaNameInsightsQueryHandler
        >();
    }

    public static void RegisterHouseholdsAndRelationshipsHandlers(IServiceCollection services)
    {
        RegisterHouseholdHandlers(services);
        RegisterRelationshipHandlers(services);
    }

    private static void RegisterHouseholdHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateHouseholdCommand,
            HouseholdResponse,
            CreateHouseholdCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateHouseholdCommand,
            HouseholdResponse,
            UpdateHouseholdCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddHouseholdMemberCommand,
            HouseholdResponse,
            AddHouseholdMemberCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveHouseholdMemberCommand,
            HouseholdResponse,
            RemoveHouseholdMemberCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetHouseholdLocationCommand,
            HouseholdResponse,
            SetHouseholdLocationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetHouseholdByIdQuery,
            HouseholdResponse,
            GetHouseholdByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonHouseholdsQuery,
            IReadOnlyList<HouseholdResponse>,
            GetPersonHouseholdsQueryHandler
        >();
    }

    private static void RegisterRelationshipHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            AddKnownRelationshipCommand,
            KnownRelationshipResponse,
            AddKnownRelationshipCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveKnownRelationshipCommand,
            bool,
            RemoveKnownRelationshipCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonKnownRelationshipsQuery,
            IReadOnlyList<KnownRelationshipResponse>,
            GetPersonKnownRelationshipsQueryHandler
        >();
        _ = services.AddCommandHandler<
            UpsertPeerNetworkEdgeCommand,
            PeerNetworkEdgeResponse,
            UpsertPeerNetworkEdgeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonPeerNetworkQuery,
            IReadOnlyList<PeerNetworkEdgeResponse>,
            GetPersonPeerNetworkQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonRelationshipGraphQuery,
            PersonRelationshipGraphResponse,
            GetPersonRelationshipGraphQueryHandler
        >();
    }

    public static void RegisterDuplicateAndMergeHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<PersonDuplicateScannerService>();
        _ = services.AddCommandHandler<
            RunPersonDuplicateScanCommand,
            DuplicateScanSummaryResponse,
            RunPersonDuplicateScanCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonDuplicatesQuery,
            IReadOnlyList<PersonDuplicateResponse>,
            GetPersonDuplicatesQueryHandler
        >();
        _ = services.AddCommandHandler<
            MarkDuplicateStatusCommand,
            PersonDuplicateResponse,
            MarkDuplicateStatusCommandHandler
        >();
        _ = services.AddCommandHandler<
            MergePeopleCommand,
            PersonMergeResponse,
            MergePeopleCommandHandler
        >();
        _ = services.AddQueryHandler<
            ResolveCanonicalPersonIdQuery,
            CanonicalPersonResolutionResponse,
            ResolveCanonicalPersonIdQueryHandler
        >();
    }
}
