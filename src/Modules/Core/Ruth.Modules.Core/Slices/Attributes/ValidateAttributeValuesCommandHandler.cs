using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class ValidateAttributeValuesCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes
) : ICommandHandler<ValidateAttributeValuesCommand, AttributeValidationResponse>
{
    public async Task<Result<AttributeValidationResponse>> HandleAsync(
        ValidateAttributeValuesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var typeKey = command.EntityTypeKey?.Trim() ?? string.Empty;
        var descriptor = await dbContext
            .EntityTypeDescriptors.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Key == typeKey, cancellationToken);
        if (descriptor is null)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        var qualCol = string.IsNullOrWhiteSpace(command.QualifierColumn)
            ? null
            : command.QualifierColumn.Trim();
        var qualVal = string.IsNullOrWhiteSpace(command.QualifierValue)
            ? null
            : command.QualifierValue.Trim();

        var definitions = await dbContext
            .AttributeDefinitions.AsNoTracking()
            .Where(a =>
                a.EntityTypeId == descriptor.Id
                && a.IsActive
                && (
                    a.EntityTypeQualifierColumn == null
                    || (
                        a.EntityTypeQualifierColumn == qualCol
                        && a.EntityTypeQualifierValue == qualVal
                    )
                )
            )
            .OrderBy(static a => a.Order)
            .ToListAsync(cancellationToken);

        var templates = await dbContext
            .AttributeMatrixTemplates.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id.Value, cancellationToken);

        return AttributeValueValidator.Validate(definitions, command.Values, fieldTypes, templates);
    }
}
