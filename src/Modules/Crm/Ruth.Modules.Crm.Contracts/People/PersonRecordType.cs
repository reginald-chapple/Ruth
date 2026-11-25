namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Classification of a CRM record.</summary>
public enum PersonRecordType
{
    /// <summary>An individual person record.</summary>
    Person = 0,

    /// <summary>A business or organization entity.</summary>
    Business = 1,

    /// <summary>A non-human REST API service user record.</summary>
    RestUser = 2,

    /// <summary>An anonymous or nameless person placeholder.</summary>
    Nameless = 3,
}
