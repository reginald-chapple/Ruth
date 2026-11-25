using System.Runtime.InteropServices;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Represents an RFC 5545 BYDAY token (e.g., SU, 1SU, -1FR).</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ByDayRule(DayOfWeek DayOfWeek, int? Ordinal = null);
