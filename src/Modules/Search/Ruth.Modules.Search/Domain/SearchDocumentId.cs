using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Search.Domain;

public readonly record struct SearchDocumentId(Guid Value) : IStronglyTypedId<SearchDocumentId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SearchDocumentId Empty => new(Guid.Empty);

    public static SearchDocumentId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static SearchDocumentId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
