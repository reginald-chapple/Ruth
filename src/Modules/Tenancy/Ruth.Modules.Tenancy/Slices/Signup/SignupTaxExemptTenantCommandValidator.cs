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

        var slugResult = TenantSlugPolicy.NormalizeAndValidateSlug(request.Slug);
        if (slugResult.IsFailure)
        {
            errors.AddRange(slugResult.Errors);
        }

        var einResult = EinFormatter.NormalizeAndValidate(request.Ein);
        if (einResult.IsFailure)
        {
            errors.AddRange(einResult.Errors);
        }

        var subsectionResult = TenantSlugPolicy.NormalizeAndValidateSubsection(
            request.IrsSubsectionCode
        );
        if (subsectionResult.IsFailure)
        {
            errors.AddRange(subsectionResult.Errors);
        }

        ValidateAdminInputs(request, errors);
        return ValueTask.FromResult<IReadOnlyList<DomainError>>(errors);
    }

    private static void ValidateAdminInputs(
        SignupTaxExemptTenantCommand request,
        List<DomainError> errors
    )
    {
        if (
            string.IsNullOrWhiteSpace(request.AdminUserName)
            || request.AdminUserName.Trim().Length < 3
        )
        {
            errors.Add(
                DomainError.Validation(
                    "Tenancy.InvalidAdminUserName",
                    "Administrator username must be at least 3 characters."
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
