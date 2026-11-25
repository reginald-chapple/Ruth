using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.BackgroundChecks;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Domain.Verification;
using Ruth.Modules.Crm.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Crm.Infrastructure;

/// <summary>EF Core database context exclusively owning the crm PostgreSQL schema.</summary>
public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the CRM module.</summary>
    public const string SchemaName = "crm";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the people table.</summary>
    public DbSet<Person> People => Set<Person>();

    /// <summary>Gets the person phone numbers table.</summary>
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    /// <summary>Gets the person previous last names table.</summary>
    public DbSet<PersonPreviousName> PersonPreviousNames => Set<PersonPreviousName>();

    /// <summary>Gets the person alternate search keys table.</summary>
    public DbSet<PersonSearchKey> PersonSearchKeys => Set<PersonSearchKey>();

    /// <summary>Gets the Double Metaphone index table.</summary>
    public DbSet<MetaphoneEntry> Metaphones => Set<MetaphoneEntry>();

    /// <summary>Gets the statistical first-name gender reference table.</summary>
    public DbSet<MetaFirstNameGenderLookup> MetaFirstNameGenderLookups =>
        Set<MetaFirstNameGenderLookup>();

    /// <summary>Gets the linguistic nickname expansion reference table.</summary>
    public DbSet<MetaNickNameLookup> MetaNickNameLookups => Set<MetaNickNameLookup>();

    /// <summary>Gets the households (families) table.</summary>
    public DbSet<Household> Households => Set<Household>();

    /// <summary>Gets the household memberships table.</summary>
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();

    /// <summary>Gets the household physical locations table.</summary>
    public DbSet<HouseholdLocation> HouseholdLocations => Set<HouseholdLocation>();

    /// <summary>Gets the known relationships table.</summary>
    public DbSet<KnownRelationship> KnownRelationships => Set<KnownRelationship>();

    /// <summary>Gets the scored peer-network edges table.</summary>
    public DbSet<PeerNetworkEdge> PeerNetworkEdges => Set<PeerNetworkEdge>();

    /// <summary>Gets the detected person duplicate pairs table.</summary>
    public DbSet<PersonDuplicate> PersonDuplicates => Set<PersonDuplicate>();

    /// <summary>Gets the merged person redirect ledger table.</summary>
    public DbSet<PersonRedirect> PersonRedirects => Set<PersonRedirect>();

    /// <summary>Gets the signal types table.</summary>
    public DbSet<SignalType> SignalTypes => Set<SignalType>();

    /// <summary>Gets the person signals table.</summary>
    public DbSet<PersonSignal> PersonSignals => Set<PersonSignal>();

    /// <summary>Gets the badge definitions table.</summary>
    public DbSet<BadgeDefinition> BadgeDefinitions => Set<BadgeDefinition>();

    /// <summary>Gets the assessment types table.</summary>
    public DbSet<AssessmentType> AssessmentTypes => Set<AssessmentType>();

    /// <summary>Gets the person assessments table.</summary>
    public DbSet<Assessment> Assessments => Set<Assessment>();

    /// <summary>Gets the background checks table.</summary>
    public DbSet<BackgroundCheck> BackgroundChecks => Set<BackgroundCheck>();

    /// <summary>Gets the identity verification codes pool table.</summary>
    public DbSet<IdentityVerificationCode> IdentityVerificationCodes =>
        Set<IdentityVerificationCode>();

    /// <summary>Gets the identity verification challenges table.</summary>
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();

    /// <summary>Gets the person profile viewed audit log table.</summary>
    public DbSet<PersonViewed> PersonViewedEntries => Set<PersonViewed>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasPostgresExtension("pg_trgm");
        CrmModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
