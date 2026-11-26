using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain.Assistance;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal static class AssistanceMapper
{
    public static AssistanceProgramResponse ToResponse(AssistanceProgram entity)
    {
        return new AssistanceProgramResponse(
            entity.Id.Value,
            entity.Name,
            entity.Code,
            entity.Category,
            entity.FundingAccountId.Value,
            entity.CurrencyCode,
            entity.TotalBudgetAmount,
            entity.DisbursedAmount,
            entity.RemainingBudgetAmount,
            entity.MaxAwardPerCase,
            entity.Description,
            entity.RequiresDocumentation,
            entity.BranchId,
            entity.StartDate,
            entity.EndDate,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static AssistanceCaseResponse ToResponse(
        AssistanceCase entity,
        string programName,
        AssistanceProgramCategory programCategory
    )
    {
        return new AssistanceCaseResponse(
            entity.Id.Value,
            entity.CaseNumber,
            entity.ProgramId.Value,
            programName,
            programCategory,
            entity.ApplicantPersonId,
            entity.HouseholdId,
            entity.AssignedCaseworkerPersonId,
            entity.BranchId,
            entity.Status,
            entity.Summary,
            entity.RequestDetails,
            entity.DecisionNotes,
            entity.CurrencyCode,
            entity.RequestedAmount,
            entity.ApprovedAmount,
            entity.DisbursedAmount,
            entity.RemainingApprovedAmount,
            MapDocuments(entity),
            MapDisbursements(entity),
            new Dictionary<string, string?>(
                entity.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            entity.SubmittedAtUtc,
            entity.ReviewedAtUtc,
            entity.ClosedAtUtc
        );
    }

    private static AssistanceDocumentResponse[] MapDocuments(AssistanceCase entity)
    {
        return
        [
            .. entity
                .Documents.OrderBy(static d => d.UploadedAtUtc)
                .Select(static d => new AssistanceDocumentResponse(
                    d.Id.Value,
                    d.CaseId.Value,
                    d.Title,
                    d.DocumentCategory,
                    d.BinaryFileId,
                    d.UploadedByPersonId,
                    d.IsVerified,
                    d.UploadedAtUtc
                )),
        ];
    }

    private static AssistanceDisbursementResponse[] MapDisbursements(AssistanceCase entity)
    {
        return
        [
            .. entity
                .Disbursements.OrderBy(static d => d.DisbursedAtUtc)
                .Select(static d => new AssistanceDisbursementResponse(
                    d.Id.Value,
                    d.CaseId.Value,
                    d.DisbursementMethod,
                    d.Amount,
                    d.CurrencyCode,
                    d.PayeeName,
                    d.TargetInvoiceId?.Value,
                    d.ReferenceNumber,
                    d.Notes,
                    d.ApprovedByPersonId,
                    d.DisbursedAtUtc
                )),
        ];
    }
}
