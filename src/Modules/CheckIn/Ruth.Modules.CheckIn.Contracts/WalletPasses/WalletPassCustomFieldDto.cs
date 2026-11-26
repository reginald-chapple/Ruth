namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Auxiliary key-label-value field displayed on a smart wallet pass.</summary>
/// <param name="Key">Stable machine field identifier (2..40 characters).</param>
/// <param name="Label">Short uppercase/caption field label shown on the pass.</param>
/// <param name="Value">Formatted text value displayed under the label.</param>
public sealed record WalletPassCustomFieldDto(string Key, string Label, string Value);
