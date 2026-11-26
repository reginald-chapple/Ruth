using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Communication.Infrastructure;

/// <summary>EF Core database context exclusively owning the comms PostgreSQL schema.</summary>
public sealed class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Communication module.</summary>
    public const string SchemaName = "comms";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the reusable template snippets table.</summary>
    public DbSet<TemplateSnippet> TemplateSnippets => Set<TemplateSnippet>();

    /// <summary>Gets the transactional system communications table.</summary>
    public DbSet<SystemCommunication> SystemCommunications => Set<SystemCommunication>();

    /// <summary>Gets the transactional system notification delivery logs table.</summary>
    public DbSet<SystemNotificationLog> SystemNotificationLogs => Set<SystemNotificationLog>();

    /// <summary>Gets the organization-owned E.164 phone numbers table.</summary>
    public DbSet<SystemPhoneNumber> SystemPhoneNumbers => Set<SystemPhoneNumber>();

    /// <summary>Gets the registered personal push notification devices table.</summary>
    public DbSet<PersonalDevice> PersonalDevices => Set<PersonalDevice>();

    /// <summary>Gets the reusable bulk communication templates table.</summary>
    public DbSet<CommunicationTemplate> CommunicationTemplates => Set<CommunicationTemplate>();

    /// <summary>Gets the bulk and targeted communications table.</summary>
    public DbSet<Domain.BulkCommunications.Communication> Communications =>
        Set<Domain.BulkCommunications.Communication>();

    /// <summary>Gets the per-recipient communication delivery and telemetry records table.</summary>
    public DbSet<CommunicationRecipient> CommunicationRecipients => Set<CommunicationRecipient>();

    /// <summary>Gets the recipient opt-out and topic subscription preferences table.</summary>
    public DbSet<CommunicationSubscriptionPreference> CommunicationSubscriptionPreferences =>
        Set<CommunicationSubscriptionPreference>();

    /// <summary>Gets the inbound SMS keyword responder pipelines table.</summary>
    public DbSet<SmsPipeline> SmsPipelines => Set<SmsPipeline>();

    /// <summary>Gets the SMS keyword pipeline actions table.</summary>
    public DbSet<SmsAction> SmsActions => Set<SmsAction>();

    /// <summary>Gets the two-way SMS conversation messages table.</summary>
    public DbSet<CommunicationResponse> CommunicationResponses => Set<CommunicationResponse>();

    /// <summary>Gets the multi-step automated drip communication flows table.</summary>
    public DbSet<CommunicationFlow> CommunicationFlows => Set<CommunicationFlow>();

    /// <summary>Gets the communication flow cohort execution instances table.</summary>
    public DbSet<CommunicationFlowInstance> CommunicationFlowInstances =>
        Set<CommunicationFlowInstance>();

    /// <summary>Gets the enrolled communication flow recipients table.</summary>
    public DbSet<CommunicationFlowInstanceRecipient> CommunicationFlowInstanceRecipients =>
        Set<CommunicationFlowInstanceRecipient>();

    /// <summary>Gets the attributed communication flow conversions table.</summary>
    public DbSet<CommunicationFlowConversion> CommunicationFlowConversions =>
        Set<CommunicationFlowConversion>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new TemplateSnippetConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SystemCommunicationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SystemNotificationLogConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SystemPhoneNumberConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonalDeviceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationTemplateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationRecipientConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationSubscriptionPreferenceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SmsPipelineConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SmsActionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationResponseConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationFlowConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationFlowInstanceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationFlowInstanceRecipientConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommunicationFlowConversionConfiguration());
    }
}
