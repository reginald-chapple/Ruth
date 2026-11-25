using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Query to retrieve a person's full 360 profile by ID (automatically following merge redirects when requested).</summary>
public sealed record GetPersonByIdQuery(Guid PersonId, bool FollowRedirects = true)
    : IQuery<PersonDetailResponse>;
