namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Input control and validation kind for a dynamic registration form field.</summary>
public enum RegistrationFieldKind
{
    /// <summary>Single-line or multi-line free text input.</summary>
    Text = 0,

    /// <summary>Email address input.</summary>
    Email = 1,

    /// <summary>Phone number input.</summary>
    Phone = 2,

    /// <summary>Single-choice dropdown or radio selection.</summary>
    SingleSelect = 3,

    /// <summary>Multi-choice selection.</summary>
    MultiSelect = 4,

    /// <summary>Yes/No checkbox or toggle.</summary>
    YesNo = 5,

    /// <summary>Calendar date input.</summary>
    Date = 6,

    /// <summary>Numeric whole-number input.</summary>
    WholeNumber = 7,
}
