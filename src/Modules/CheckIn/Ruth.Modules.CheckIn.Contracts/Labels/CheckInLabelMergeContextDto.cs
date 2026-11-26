namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Merge-field context supplied when compiling a check-in label to Zebra ZPL II or visual preview.</summary>
/// <param name="ParticipantFullName">Participant full name.</param>
/// <param name="ParticipantNickName">Participant preferred first/nickname.</param>
/// <param name="ParticipantLastName">Participant last name.</param>
/// <param name="SecurityPickupCode">Issued daily guardian pickup code.</param>
/// <param name="AreaName">Assigned check-in area name.</param>
/// <param name="LocationName">Assigned room or zone name.</param>
/// <param name="ScheduleName">Active schedule display label.</param>
/// <param name="ServiceDateText">Formatted service date.</param>
/// <param name="CheckedInTimeText">Formatted check-in time.</param>
/// <param name="MedicalAllergyAlert">Medical or allergy alert banner text.</param>
/// <param name="ComplianceAlert">Safety/compliance signal alert text.</param>
/// <param name="AccommodationNote">Accommodation or check-in note.</param>
/// <param name="GuardianReceiptSummary">Summary of checked-in participants and rooms for guardian receipts.</param>
/// <param name="MembershipTier">Participant membership tier.</param>
/// <param name="BadgeIconCodes">Badge or tier icon tokens.</param>
/// <param name="BarcodeValue">1D barcode or QR scan payload.</param>
public sealed record CheckInLabelMergeContextDto(
    string ParticipantFullName = "Jordan Rivera",
    string ParticipantNickName = "Jordan",
    string ParticipantLastName = "Rivera",
    string? SecurityPickupCode = "BC47",
    string AreaName = "Elementary STEM Lab",
    string LocationName = "Room 101",
    string ScheduleName = "9:00 AM Session",
    string ServiceDateText = "2026-11-25",
    string CheckedInTimeText = "08:54 AM",
    string? MedicalAllergyAlert = null,
    string? ComplianceAlert = null,
    string? AccommodationNote = null,
    string? GuardianReceiptSummary = null,
    string? MembershipTier = null,
    IReadOnlyList<string>? BadgeIconCodes = null,
    string? BarcodeValue = null
);
