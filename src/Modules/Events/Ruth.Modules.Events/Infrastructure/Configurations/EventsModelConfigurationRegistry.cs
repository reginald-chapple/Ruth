using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal static class EventsModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyCalendarConfigurations(modelBuilder);
        ApplyInteractiveSessionConfigurations(modelBuilder);
        ApplyRegistrationTemplateConfigurations(modelBuilder);
        ApplyRegistrationExecutionConfigurations(modelBuilder);
    }

    private static void ApplyCalendarConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new EventCalendarEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EventItemEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EventCalendarItemEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EventItemAudienceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EventItemOccurrenceEntityConfiguration());
    }

    private static void ApplyInteractiveSessionConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new InteractiveSessionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractiveActionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractiveSessionOccurrenceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractiveResponseEntityConfiguration());
    }

    private static void ApplyRegistrationTemplateConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateFormEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateFormFieldEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateFeeEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateFeeItemEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplateDiscountEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationTemplatePlacementEntityConfiguration());
    }

    private static void ApplyRegistrationExecutionConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new RegistrationInstanceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationSessionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RegistrationRegistrantEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(
            new RegistrationRegistrantFeeSelectionEntityConfiguration()
        );
        _ = modelBuilder.ApplyConfiguration(new RegistrationPaymentRecordEntityConfiguration());
    }
}
