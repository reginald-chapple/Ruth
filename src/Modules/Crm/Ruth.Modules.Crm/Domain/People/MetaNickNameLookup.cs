using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Read-only first-name to nickname linguistic lookup entry used for name search expansion.</summary>
public sealed class MetaNickNameLookup : ITenantScoped
{
    private MetaNickNameLookup() { }

    /// <summary>Creates a nickname lookup row.</summary>
    public MetaNickNameLookup(
        Guid id,
        string firstName,
        string nickName,
        string genderCode,
        int count = 100
    )
    {
        Id = id;
        FirstName = firstName.Trim();
        NormalizedFirstName = FirstName.ToUpperInvariant();
        NickName = nickName.Trim();
        NormalizedNickName = NickName.ToUpperInvariant();
        GenderCode = genderCode;
        Count = Math.Max(1, count);
    }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the formal first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase formal first name.</summary>
    public string NormalizedFirstName { get; private set; } = string.Empty;

    /// <summary>Gets the nickname variant.</summary>
    public string NickName { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase nickname variant.</summary>
    public string NormalizedNickName { get; private set; } = string.Empty;

    /// <summary>Gets the single-letter gender code (M, F, or U).</summary>
    public string GenderCode { get; private set; } = "U";

    /// <summary>Gets the frequency weight.</summary>
    public int Count { get; private set; }

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign MetaNickNameLookup ({Id}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }
}
