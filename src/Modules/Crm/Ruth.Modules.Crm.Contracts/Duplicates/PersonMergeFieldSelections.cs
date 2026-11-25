namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Per-field winner selection for side-by-side person merges.</summary>
public sealed record PersonMergeFieldSelections(
    PersonMergeFieldChoice FirstName = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice NickName = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice MiddleName = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice LastName = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice Title = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice Suffix = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice Email = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice Gender = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice MaritalStatus = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice BirthDate = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice GraduationYear = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice ConnectionStatus = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice Photo = PersonMergeFieldChoice.Surviving,
    PersonMergeFieldChoice PrimaryCampus = PersonMergeFieldChoice.Surviving
);
