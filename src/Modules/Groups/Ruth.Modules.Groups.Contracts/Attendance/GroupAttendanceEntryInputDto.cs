namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Individual person attendance entry submitted in a group roster attendance save.</summary>
public sealed record GroupAttendanceEntryInputDto(
    Guid PersonId,
    bool DidAttend,
    string? Note = null
);
