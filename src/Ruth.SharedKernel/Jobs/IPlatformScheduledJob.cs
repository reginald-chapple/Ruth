namespace Ruth.SharedKernel.Jobs;

/// <summary>
/// Marker interface for platform-level <see cref="IScheduledJob" /> implementations that manage
/// control-plane or cross-schema infrastructure directly rather than being invoked once per active tenant.
/// </summary>
public interface IPlatformScheduledJob;
