using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Query to download and authenticate-decrypt a stored check or remittance image.</summary>
/// <param name="TransactionId">Parent financial transaction identifier.</param>
/// <param name="ImageId">Target transaction image identifier.</param>
public sealed record DownloadDecryptedCheckImageQuery(Guid TransactionId, Guid ImageId)
    : IQuery<DecryptedCheckImageResponse>;
