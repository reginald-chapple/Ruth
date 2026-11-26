using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Events.Infrastructure;

/// <summary>EF Core database context exclusively owning the events PostgreSQL schema.</summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "events";

    protected override string Schema => SchemaName;

    public DbSet<EventCalendar> EventCalendars => Set<EventCalendar>();
    public DbSet<EventItem> EventItems => Set<EventItem>();
    public DbSet<EventCalendarItem> EventCalendarItems => Set<EventCalendarItem>();
    public DbSet<EventItemAudience> EventItemAudiences => Set<EventItemAudience>();
    public DbSet<EventItemOccurrence> EventItemOccurrences => Set<EventItemOccurrence>();

    public DbSet<InteractiveSession> InteractiveSessions => Set<InteractiveSession>();
    public DbSet<InteractiveAction> InteractiveActions => Set<InteractiveAction>();
    public DbSet<InteractiveSessionOccurrence> InteractiveSessionOccurrences =>
        Set<InteractiveSessionOccurrence>();
    public DbSet<InteractiveResponse> InteractiveResponses => Set<InteractiveResponse>();

    public DbSet<RegistrationTemplate> RegistrationTemplates => Set<RegistrationTemplate>();
    public DbSet<RegistrationTemplateForm> RegistrationTemplateForms =>
        Set<RegistrationTemplateForm>();
    public DbSet<RegistrationTemplateFormField> RegistrationTemplateFormFields =>
        Set<RegistrationTemplateFormField>();
    public DbSet<RegistrationTemplateFee> RegistrationTemplateFees =>
        Set<RegistrationTemplateFee>();
    public DbSet<RegistrationTemplateFeeItem> RegistrationTemplateFeeItems =>
        Set<RegistrationTemplateFeeItem>();
    public DbSet<RegistrationTemplateDiscount> RegistrationTemplateDiscounts =>
        Set<RegistrationTemplateDiscount>();
    public DbSet<RegistrationTemplatePlacement> RegistrationTemplatePlacements =>
        Set<RegistrationTemplatePlacement>();

    public DbSet<RegistrationInstance> RegistrationInstances => Set<RegistrationInstance>();
    public DbSet<RegistrationSession> RegistrationSessions => Set<RegistrationSession>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<RegistrationRegistrant> RegistrationRegistrants => Set<RegistrationRegistrant>();
    public DbSet<RegistrationRegistrantFeeSelection> RegistrationRegistrantFeeSelections =>
        Set<RegistrationRegistrantFeeSelection>();
    public DbSet<RegistrationPaymentRecord> RegistrationPaymentRecords =>
        Set<RegistrationPaymentRecord>();

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.EventsModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
