using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Slices.Signup;

internal sealed class SignupTaxExemptTenantCommandValidator
    : IRequestValidator<SignupTaxExemptTenantCommand>
{
    public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
        SignupTaxExemptTenantCommand request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<DomainError>();

        ValidateOrganizationInputs(request, errors);
        ValidateAdminInputs(request, errors);
        return ValueTask.FromResult<IReadOnlyList<DomainError>>(errors);
    }

    private static void ValidateOrganizationInputs(
        SignupTaxExemptTenantCommand request,
        List<DomainError> errors
    )
    {
        if (string.IsNullOrWhiteSpace(request.LegalName) || request.LegalName.Trim().Length > 200)
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.InvalidLegalName",
                    "Legal organization name is required (maximum 200 characters)."
                )
            );
        }

        if (
            !string.IsNullOrWhiteSpace(request.DisplayName)
            && request.DisplayName.Trim().Length > 200
        )
        {
            errors.Add(TenancyErrors.InvalidDisplayName);
        }

        if (
            !string.IsNullOrWhiteSpace(request.PrimaryBranchName)
            && request.PrimaryBranchName.Trim().Length > 160
        )
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.InvalidPrimaryBranchName",
                    "Primary operating site name must not exceed 160 characters."
                )
            );
        }

        AddIfFailure(TenantSlugPolicy.NormalizeAndValidateSlug(request.Slug), errors);
        AddIfFailure(EinFormatter.NormalizeAndValidate(request.Ein), errors);
        AddIfFailure(
            TenantSlugPolicy.NormalizeAndValidateSubsection(request.IrsSubsectionCode),
            errors
        );
        AddIfFailure(TenantSlugPolicy.NormalizeAndValidateStateCode(request.StateCode), errors);
        AddIfFailure(TenantSlugPolicy.NormalizeAndValidateTimeZoneId(request.TimeZoneId), errors);
    }

    private static void AddIfFailure<T>(Result<T> result, List<DomainError> errors)
    {
        if (result.IsFailure)
        {
            errors.AddRange(result.Errors);
        }
    }

    private static void ValidateAdminInputs(
        SignupTaxExemptTenantCommand request,
        List<DomainError> errors
    )
    {
        if (
            string.IsNullOrWhiteSpace(request.AdminUserName)
            || request.AdminUserName.Trim().Length is < 3 or > 100
        )
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.InvalidAdminUserName",
                    "Administrator username must be between 3 and 100 characters."
                )
            );
        }

        if (
            string.IsNullOrWhiteSpace(request.AdminEmail)
            || !request.AdminEmail.Contains('@', StringComparison.Ordinal)
        )
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.InvalidAdminEmail",
                    "A valid administrator email address is required."
                )
            );
        }

        if (string.IsNullOrWhiteSpace(request.AdminPassword) || request.AdminPassword.Length < 12)
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.WeakAdminPassword",
                    "Administrator password must be at least 12 characters."
                )
            );
        }
    }
}
