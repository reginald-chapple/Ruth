using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal static class AuthenticationSliceRegistration
{
    public static IServiceCollection AddAuthenticationHandlers(this IServiceCollection services)
    {
        RegisterSessionHandlers(services);
        RegisterAccountAndOidcHandlers(services);
        return services;
    }

    private static void RegisterSessionHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<LoginCommand, AuthTokensResponse, LoginCommandHandler>();
        _ = services.AddCommandHandler<
            RefreshAccessTokenCommand,
            AuthTokensResponse,
            RefreshAccessTokenCommandHandler
        >();
        _ = services.AddCommandHandler<LogoutCommand, bool, LogoutCommandHandler>();
        _ = services.AddQueryHandler<
            GetCurrentUserQuery,
            AuthenticatedUserResponse,
            GetCurrentUserQueryHandler
        >();
        _ = services.AddCommandHandler<ChangePasswordCommand, bool, ChangePasswordCommandHandler>();
        _ = services.AddCommandHandler<
            RequestPasswordResetCommand,
            IssuedTokenResponse,
            RequestPasswordResetCommandHandler
        >();
        _ = services.AddCommandHandler<
            ConfirmPasswordResetCommand,
            bool,
            ConfirmPasswordResetCommandHandler
        >();
        _ = services.AddCommandHandler<
            RequestAccountConfirmationCommand,
            IssuedTokenResponse,
            RequestAccountConfirmationCommandHandler
        >();
        _ = services.AddCommandHandler<ConfirmAccountCommand, bool, ConfirmAccountCommandHandler>();
    }

    private static void RegisterAccountAndOidcHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateUserAccountCommand,
            UserAccountSummaryResponse,
            CreateUserAccountCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetUserAccountsQuery,
            IReadOnlyList<UserAccountSummaryResponse>,
            GetUserAccountsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetLoginHistoryQuery,
            IReadOnlyList<LoginHistoryItemResponse>,
            GetLoginHistoryQueryHandler
        >();
        _ = services.AddCommandHandler<
            UpsertExternalAuthProviderCommand,
            ExternalAuthProviderResponse,
            UpsertExternalAuthProviderCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetExternalAuthProvidersQuery,
            IReadOnlyList<ExternalAuthProviderResponse>,
            GetExternalAuthProvidersQueryHandler
        >();
        _ = services.AddCommandHandler<
            StartOidcLoginCommand,
            StartOidcLoginResponse,
            StartOidcLoginCommandHandler
        >();
        _ = services.AddCommandHandler<
            CompleteOidcLoginCommand,
            AuthTokensResponse,
            CompleteOidcLoginCommandHandler
        >();
    }
}
