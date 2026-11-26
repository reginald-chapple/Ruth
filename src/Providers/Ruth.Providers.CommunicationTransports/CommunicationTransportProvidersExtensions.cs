using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Providers.CommunicationTransports.Mailgun;
using Ruth.Providers.CommunicationTransports.SendGrid;
using Ruth.Providers.CommunicationTransports.Smtp;
using Ruth.Providers.CommunicationTransports.Twilio;

namespace Ruth.Providers.CommunicationTransports;

/// <summary>Registers external SMTP, SendGrid, Mailgun, and Twilio SMS transport providers.</summary>
public static class CommunicationTransportProvidersExtensions
{
    /// <summary>Adds external email and SMS communication transport providers to DI.</summary>
    public static IServiceCollection AddCommunicationTransportProviders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<SmtpTransportOptions>(
            configuration.GetSection(SmtpTransportOptions.SectionName)
        );
        _ = services.Configure<SendGridTransportOptions>(
            configuration.GetSection(SendGridTransportOptions.SectionName)
        );
        _ = services.Configure<MailgunTransportOptions>(
            configuration.GetSection(MailgunTransportOptions.SectionName)
        );
        _ = services.Configure<TwilioSmsTransportOptions>(
            configuration.GetSection(TwilioSmsTransportOptions.SectionName)
        );

        _ = services.AddHttpClient(SendGridCommunicationTransport.HttpClientName);
        _ = services.AddHttpClient(MailgunCommunicationTransport.HttpClientName);
        _ = services.AddHttpClient(TwilioSmsCommunicationTransport.HttpClientName);

        _ = services.AddSingleton<ICommunicationTransport, SmtpCommunicationTransport>();
        _ = services.AddSingleton<ICommunicationTransport, SendGridCommunicationTransport>();
        _ = services.AddSingleton<ICommunicationTransport, MailgunCommunicationTransport>();
        _ = services.AddSingleton<ICommunicationTransport, TwilioSmsCommunicationTransport>();

        return services;
    }
}
