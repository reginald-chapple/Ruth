namespace Ruth.Persistence.EntityFramework;

/// <summary>Configuration options for PostgreSQL 18 Row-Level Security dual-role enforcement.</summary>
public sealed class RlsOptions
{
    /// <summary>Gets or sets whether tenant-scoped <see cref="ModuleDbContext" />s connect via the non-superuser runtime role.</summary>
    public bool UseNonSuperuserAppRole { get; set; } = true;

    /// <summary>Gets or sets the non-superuser schema-owner/DDL PostgreSQL role name (ruth_migrator).</summary>
    public string MigratorRoleName { get; set; } = "ruth_migrator";

    /// <summary>Gets or sets an optional password provisioned for <see cref="MigratorRoleName" /> during bootstrap.</summary>
    public string MigratorRolePassword { get; set; } = "ruth_migrator_local_dev_pw";

    /// <summary>Gets or sets the non-superuser, NOBYPASSRLS PostgreSQL login role used by tenant contexts (ruth_app).</summary>
    public string AppRoleName { get; set; } = "ruth_app";

    /// <summary>Gets or sets the password provisioned and used for <see cref="AppRoleName" />.</summary>
    public string AppRolePassword { get; set; } = "ruth_app_rls_local_dev_pw";
}
