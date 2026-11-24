namespace Ruth.SharedKernel.Results;

/// <summary>Read-only contract shared by <see cref="Result" /> and <see cref="Result{TValue}" />.</summary>
public interface IResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    bool IsFailure { get; }

    /// <summary>Gets the primary error when failed, or <see cref="DomainError.None" /> when successful.</summary>
    DomainError PrimaryError { get; }

    /// <summary>Gets all errors produced by a failed operation, or an empty list when successful.</summary>
    IReadOnlyList<DomainError> Errors { get; }
}
