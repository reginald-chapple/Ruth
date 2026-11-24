using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Onboarding;

internal sealed class CompleteOnboardingCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHashingService passwordHashingService,
    IClock clock,
    IOnboardingStateReader stateReader
) : ICommandHandler<CompleteOnboardingCommand, CompleteOnboardingResponse>
{
    private readonly IdentityDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IPasswordHashingService _passwordHashingService = passwordHashingService
        ?? throw new ArgumentNullException(nameof(passwordHashingService));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly IOnboardingStateReader _stateReader = stateReader
        ?? throw new ArgumentNullException(nameof(stateReader));

    public async Task<Result<CompleteOnboardingResponse>> HandleAsync(
        CompleteOnboardingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await HasExistingOnboardingAsync(cancellationToken))
        {
            return IdentityErrors.OnboardingAlreadyCompleted;
        }

        var passwordValidation = OnboardingPolicy.ValidatePasswordComplexity(command.AdminPassword);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<CompleteOnboardingResponse>(passwordValidation.Errors);
        }

        var utcNow = _clock.UtcNow;
        var passwordHash = _passwordHashingService.HashPassword(command.AdminPassword);
        var adminResult = UserAccount.CreateAdministrator(
            command.AdminUserName,
            command.AdminEmail,
            command.AdminDisplayName,
            passwordHash,
            utcNow
        );
        var orgResult = OrganizationProfile.CompleteInitialOnboarding(
            command.OrganizationName,
            command.TimeZoneId,
            command.PrimaryCampusName,
            command.PrimaryCampusShortCode,
            utcNow
        );

        var combined = Result.Combine(adminResult, orgResult);
        return combined.IsFailure
            ? Result.Failure<CompleteOnboardingResponse>(combined.Errors)
            : await PersistOnboardingAsync(adminResult.Value, orgResult.Value, cancellationToken);
    }

    private async Task<bool> HasExistingOnboardingAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.OrganizationProfiles.AsNoTracking().AnyAsync(cancellationToken)
            || await _dbContext.UserAccounts.AsNoTracking().AnyAsync(cancellationToken);
    }

    private async Task<Result<CompleteOnboardingResponse>> PersistOnboardingAsync(
        UserAccount admin,
        OrganizationProfile organization,
        CancellationToken cancellationToken
    )
    {
        var existingRole = await _dbContext.SecurityRoles.FirstOrDefaultAsync(
            static role => role.Id == SecurityRole.AdministratorsRoleId,
            cancellationToken
        );
        if (existingRole is null)
        {
            _ = _dbContext.SecurityRoles.Add(SecurityRole.CreateSystemAdministrators());
        }

        _ = _dbContext.UserAccounts.Add(admin);
        _ = _dbContext.OrganizationProfiles.Add(organization);

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _dbContext.ChangeTracker.Clear();
            return IdentityErrors.OnboardingAlreadyCompleted;
        }

        _stateReader.InvalidateCache();

        return new CompleteOnboardingResponse(
            organization.Id.Value,
            admin.Id.Value,
            organization.CompletedOnUtc
        );
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && string.Equals(
                postgresException.SqlState,
                PostgresErrorCodes.UniqueViolation,
                StringComparison.Ordinal
            );
    }
}
