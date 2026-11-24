namespace Ruth.E2E.Tests.Fixtures;

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut
);
