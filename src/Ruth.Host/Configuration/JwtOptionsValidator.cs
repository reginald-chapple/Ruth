using Microsoft.Extensions.Options;

namespace Ruth.Host.Configuration;

internal sealed class JwtOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<JwtOptions>
{
    private readonly IHostEnvironment _environment = environment
        ?? throw new ArgumentNullException(nameof(environment));

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!_environment.IsProduction())
        {
            return ValidateOptionsResult.Success;
        }

        var trimmed = options.SigningKey?.Trim();
        return
            string.IsNullOrEmpty(trimmed)
            || trimmed.Length < JwtOptions.MinimumSigningKeyLength
            || string.Equals(
                trimmed,
                JwtOptions.DevelopmentDefaultSigningKey,
                StringComparison.OrdinalIgnoreCase
            )
            ? ValidateOptionsResult.Fail(
                "Authentication:Jwt:SigningKey must be configured with a unique random secret of at least 32 characters when running in Production."
            )
            : ValidateOptionsResult.Success;
    }
}
