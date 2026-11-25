using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Domain;

public sealed class TemplateSnippetDomainTests
{
    [Fact]
    public void Create_Normalizes_Key_Sanitizes_Parameters_And_Categories()
    {
        var cat1 = Guid.CreateVersion7();
        var result = TemplateSnippet.Create(
            "  CTA Button  ",
            "  CTA_Button-1 ",
            "<a href=\"{{ url }}\">{{ label }}</a>",
            description: "  Reusable button  ",
            documentation: "  Pass url and label  ",
            outputMode: TemplateOutputMode.Html,
            defaultParameters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [" label "] = "Click",
                ["   "] = "ignored",
            },
            categoryIds: [cat1, cat1, Guid.Empty]
        );

        Assert.True(result.IsSuccess);
        var snippet = result.Value;
        Assert.Equal("CTA Button", snippet.Name);
        Assert.Equal("cta_button-1", snippet.Key);
        Assert.Equal("Reusable button", snippet.Description);
        Assert.Equal("Pass url and label", snippet.Documentation);
        var singleParam = Assert.Single(snippet.DefaultParameters);
        Assert.Equal("Click", singleParam.Value);
        var singleCategory = Assert.Single(snippet.CategoryIds);
        Assert.Equal(cat1, singleCategory);
        _ = Assert.Single(snippet.DomainEvents);
        Assert.Equal(TemplateSnippet.EntityTypeKeyName, snippet.EntityTypeKey);
        Assert.Equal(snippet.Id.Value, snippet.EntityId);
        Assert.NotNull(snippet.ParentAuthority);
        Assert.False(snippet.IsAllowedByDefault("Administrate"));
    }

    [Fact]
    public void Create_Validates_Required_And_Length_Constraints()
    {
        Assert.Equal(
            CommunicationTemplateErrors.SnippetNameRequired.Code,
            TemplateSnippet.Create(" ", "valid_key", "<p>x</p>").PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetNameTooLong.Code,
            TemplateSnippet
                .Create(new string('A', TemplateSnippet.NameMaxLength + 1), "valid_key", "<p>x</p>")
                .PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetKeyInvalid.Code,
            TemplateSnippet.Create("Bad Key", "123-invalid", "<p>x</p>").PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetKeyInvalid.Code,
            TemplateSnippet.Create("Bad Key", "a", "<p>x</p>").PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetKeyInvalid.Code,
            TemplateSnippet.Create("Bad Key", " ", "<p>x</p>").PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetMarkupRequired.Code,
            TemplateSnippet.Create("Empty", "valid_key", "   ").PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetMarkupTooLong.Code,
            TemplateSnippet
                .Create("Big", "valid_key", new string('x', TemplateSnippet.MarkupMaxLength + 1))
                .PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetDescriptionTooLong.Code,
            TemplateSnippet
                .Create(
                    "Desc",
                    "valid_key",
                    "<p>ok</p>",
                    description: new string('d', TemplateSnippet.DescriptionMaxLength + 1)
                )
                .PrimaryError.Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.SnippetDocumentationTooLong.Code,
            TemplateSnippet
                .Create(
                    "Docs",
                    "valid_key",
                    "<p>ok</p>",
                    documentation: new string('d', TemplateSnippet.DocumentationMaxLength + 1)
                )
                .PrimaryError.Code
        );
    }

    [Fact]
    public void Update_And_Delete_Enforce_Invariants_And_System_Guard()
    {
        var systemSnippet = TemplateSnippet
            .Create("System Footer", "sys_footer", "<p>Footer</p>", isSystem: true)
            .Value;
        Assert.Equal(
            CommunicationTemplateErrors.SystemSnippetCannotBeDeleted.Code,
            systemSnippet.EnsureCanDelete().PrimaryError.Code
        );

        var userSnippet = TemplateSnippet
            .Create("Custom Footer", "custom_footer", "<p>Old</p>", isSystem: false)
            .Value;
        var invalidUpdate = userSnippet.Update(
            "",
            "custom_footer",
            "<p>New</p>",
            description: null,
            documentation: null,
            TemplateOutputMode.Html,
            defaultParameters: null,
            categoryIds: null,
            isActive: true
        );
        Assert.True(invalidUpdate.IsFailure);

        var okUpdate = userSnippet.Update(
            "Updated Footer",
            "updated_footer",
            "Plain {{ Name }}",
            "Desc",
            "Docs",
            TemplateOutputMode.PlainText,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["Name"] = "Ruth" },
            [],
            isActive: false
        );
        Assert.True(okUpdate.IsSuccess);
        Assert.Equal("updated_footer", userSnippet.Key);
        Assert.False(userSnippet.IsActive);
        Assert.Equal(TemplateOutputMode.PlainText, userSnippet.OutputMode);
        Assert.Equal("updated_footer", userSnippet.ToDefinition().Key);

        var stamp = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        userSnippet.SetCreated(now);
        userSnippet.SetModified(now.AddMinutes(1));
        userSnippet.SetConcurrencyStamp(stamp);
        Assert.Equal(stamp, userSnippet.ConcurrencyStamp);
        var modifiedOn = Assert.NotNull(userSnippet.ModifiedOnUtc);
        Assert.Equal(now.AddMinutes(1).ToUniversalTime(), modifiedOn);

        Assert.True(userSnippet.EnsureCanDelete().IsSuccess);
        Assert.NotEqual(TemplateSnippetId.Empty, userSnippet.Id);
        Assert.Equal(userSnippet.Id.Value.ToString("D"), userSnippet.Id.ToString());
        _ = TemplateSnippetId.From(userSnippet.Id.Value);
    }
}
