using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PhoneNumbers;

/// <summary>Lists registered SystemPhoneNumber records.</summary>
/// <param name="ActiveOnly">Whether to return only active numbers.</param>
/// <param name="SmsEnabledOnly">Whether to return only SMS-enabled numbers.</param>
public sealed record GetSystemPhoneNumbersQuery(
    bool ActiveOnly = false,
    bool SmsEnabledOnly = false
) : IQuery<IReadOnlyList<SystemPhoneNumberResponse>>;
