using System.Security.Cryptography;
using System.Text;

namespace Ruth.Modules.Finance.Infrastructure;

internal sealed class FinanceSecurityOptions
{
    public const string SectionName = "Finance:Security";

    public string BankAccountHmacSecret { get; set; } =
        "ruth-dev-finance-bank-hmac-secret-change-in-production-2026";

    public string CheckImageEncryptionKey { get; set; } =
        "ruth-dev-finance-check-aes256gcm-key-change-in-prod-2026";

    public string TestGatewayWebhookSecret { get; set; } =
        "ruth-dev-test-gateway-webhook-secret-2026";

    public string CheckImageKeyId { get; set; } = "ruth-check-key-v1";

    public byte[] GetBankAccountHmacKeyBytes()
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(BankAccountHmacSecret));
    }

    public byte[] GetCheckImageAes256KeyBytes()
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(CheckImageEncryptionKey));
    }
}
