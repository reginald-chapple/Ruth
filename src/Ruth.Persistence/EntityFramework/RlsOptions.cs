namespace Ruth.Persistence.EntityFramework;

/// <summary>Configuration options for PostgreSQL 18 Row-Level Security runtime role enforcement.</summary>
public sealed class RlsOptions
{
    /// <summary>Gets or sets whether tenant-scoped <see cref="ModuleDbContext" />s connect via the non-superuser runtime role.</summary>
    public bool UseNonSuperuserAppRole { get; set; } = true;

    /// <summary>Gets or sets the non-superuser, NOBYPASSRLS PostgreSQL login role used by tenant contexts.</summary>
    public string AppRoleName { get; set; } = "ruth_app";

    /// <summary>Gets or sets the password provisioned and used for <see cref="AppRoleName" />.</summary>
    public string AppRolePassword { get; set; } = "ruth_app_secret";
}
