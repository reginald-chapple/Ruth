using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="TemplateSnippet" />.</summary>
public readonly record struct TemplateSnippetId(Guid Value) : IStronglyTypedId<TemplateSnippetId>
{
    /// <summary>Gets an empty identifier.</summary>
    public static TemplateSnippetId Empty => new(Guid.Empty);

    /// <summary>Creates a new time-ordered UUIDv7 identifier.</summary>
    public static TemplateSnippetId Create()
    {
        return new TemplateSnippetId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static TemplateSnippetId From(Guid value)
    {
        return new TemplateSnippetId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
