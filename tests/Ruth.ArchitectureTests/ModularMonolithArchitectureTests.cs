using System.Reflection;
using NetArchTest.Rules;
using Ruth.Host;
using Ruth.Modules.CheckIn;
using Ruth.Modules.CheckIn.Contracts;
using Ruth.Modules.Commerce;
using Ruth.Modules.Commerce.Contracts;
using Ruth.Modules.Communication;
using Ruth.Modules.Communication.Contracts;
using Ruth.Modules.Core;
using Ruth.Modules.Core.Contracts;
using Ruth.Modules.Crm;
using Ruth.Modules.Crm.Contracts;
using Ruth.Modules.Events;
using Ruth.Modules.Events.Contracts;
using Ruth.Modules.Finance;
using Ruth.Modules.Finance.Contracts;
using Ruth.Modules.Groups;
using Ruth.Modules.Groups.Contracts;
using Ruth.Modules.Identity;
using Ruth.Modules.Identity.Contracts;
using Ruth.Persistence;
using Ruth.SharedKernel;
using Xunit;

namespace Ruth.ArchitectureTests;

public sealed class ModularMonolithArchitectureTests
{
    private static readonly string[] _moduleInternalNamespaces =
    [
        "Ruth.Modules.Identity.Domain",
        "Ruth.Modules.Identity.Infrastructure",
        "Ruth.Modules.Identity.Slices",
        "Ruth.Modules.Core.Domain",
        "Ruth.Modules.Core.Infrastructure",
        "Ruth.Modules.Core.Slices",
        "Ruth.Modules.Crm.Domain",
        "Ruth.Modules.Crm.Infrastructure",
        "Ruth.Modules.Crm.Slices",
        "Ruth.Modules.Groups.Domain",
        "Ruth.Modules.Groups.Infrastructure",
        "Ruth.Modules.Groups.Slices",
        "Ruth.Modules.CheckIn.Domain",
        "Ruth.Modules.CheckIn.Infrastructure",
        "Ruth.Modules.CheckIn.Slices",
        "Ruth.Modules.Communication.Domain",
        "Ruth.Modules.Communication.Infrastructure",
        "Ruth.Modules.Communication.Slices",
        "Ruth.Modules.Finance.Domain",
        "Ruth.Modules.Finance.Infrastructure",
        "Ruth.Modules.Finance.Slices",
        "Ruth.Modules.Events.Domain",
        "Ruth.Modules.Events.Infrastructure",
        "Ruth.Modules.Events.Slices",
        "Ruth.Modules.Commerce.Domain",
        "Ruth.Modules.Commerce.Infrastructure",
        "Ruth.Modules.Commerce.Slices",
    ];

    private static Assembly[] GetModuleAssemblies()
    {
        Assembly[] knownModules =
        [
            IdentityModuleAssembly.Instance,
            CoreModuleAssembly.Instance,
            CrmModuleAssembly.Instance,
            GroupsModuleAssembly.Instance,
            CheckInModuleAssembly.Instance,
            CommunicationModuleAssembly.Instance,
            FinanceModuleAssembly.Instance,
            EventsModuleAssembly.Instance,
            CommerceModuleAssembly.Instance,
        ];

        return
        [
            .. knownModules
                .Concat(
                    AppDomain
                        .CurrentDomain.GetAssemblies()
                        .Where(static assembly =>
                            IsModuleImplementationAssembly(assembly.GetName().Name)
                        )
                )
                .Distinct(),
        ];
    }

    private static Assembly[] GetModuleContractsAssemblies()
    {
        Assembly[] knownContracts =
        [
            IdentityContractsAssembly.Instance,
            CoreContractsAssembly.Instance,
            CrmContractsAssembly.Instance,
            GroupsContractsAssembly.Instance,
            CheckInContractsAssembly.Instance,
            CommunicationContractsAssembly.Instance,
            FinanceContractsAssembly.Instance,
            EventsContractsAssembly.Instance,
            CommerceContractsAssembly.Instance,
        ];

        return
        [
            .. knownContracts
                .Concat(
                    AppDomain
                        .CurrentDomain.GetAssemblies()
                        .Where(static assembly =>
                            IsModuleContractsAssembly(assembly.GetName().Name)
                        )
                )
                .Distinct(),
        ];
    }

    private static bool IsModuleImplementationAssembly(string? name)
    {
        return name is not null
            && name.StartsWith("Ruth.Modules.", StringComparison.Ordinal)
            && !name.EndsWith(".Contracts", StringComparison.Ordinal)
            && !name.EndsWith(".Tests", StringComparison.Ordinal);
    }

    private static bool IsModuleContractsAssembly(string? name)
    {
        return name is not null
            && name.StartsWith("Ruth.Modules.", StringComparison.Ordinal)
            && name.EndsWith(".Contracts", StringComparison.Ordinal);
    }

    [Fact]
    public void SharedKernel_Should_Not_Depend_On_Web_Persistence_Or_Modules()
    {
        var result = Types
            .InAssembly(SharedKernelAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.FluentUI",
                "Npgsql",
                "StackExchange.Redis",
                "Ruth.Host",
                "Ruth.Modules",
                "Ruth.Persistence",
                "Ruth.Providers"
            )
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailingTypes("Ruth.SharedKernel has forbidden dependencies", result)
        );
    }

    [Fact]
    public void Domain_Types_Should_Not_Depend_On_Infrastructure_Slices_Or_Frameworks()
    {
        var moduleAssemblies = GetModuleAssemblies();
        Assert.NotEmpty(moduleAssemblies);

        foreach (var moduleAssembly in moduleAssemblies)
        {
            var result = Types
                .InAssembly(moduleAssembly)
                .That()
                .ResideInNamespaceContaining(".Domain")
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.AspNetCore",
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.Extensions.Identity",
                    "Microsoft.FluentUI",
                    "Npgsql",
                    "StackExchange.Redis",
                    "Ruth.Persistence"
                )
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                FormatFailingTypes(
                    $"{moduleAssembly.GetName().Name} Domain layer has framework leaks",
                    result
                )
            );

            var moduleName = moduleAssembly.GetName().Name;
            var internalLayerResult = Types
                .InAssembly(moduleAssembly)
                .That()
                .ResideInNamespaceContaining(".Domain")
                .ShouldNot()
                .HaveDependencyOnAny($"{moduleName}.Infrastructure", $"{moduleName}.Slices")
                .GetResult();

            Assert.True(
                internalLayerResult.IsSuccessful,
                FormatFailingTypes(
                    $"{moduleName} Domain layer depends on Infrastructure or Slices",
                    internalLayerResult
                )
            );
        }
    }

    [Fact]
    public void Modules_Should_Only_Reference_SharedKernel_Or_Other_Module_Contracts()
    {
        var moduleAssemblies = GetModuleAssemblies();
        Assert.NotEmpty(moduleAssemblies);

        foreach (var moduleAssembly in moduleAssemblies)
        {
            var currentName = moduleAssembly.GetName().Name!;
            string[] referencedRuthModules =
            [
                .. moduleAssembly
                    .GetReferencedAssemblies()
                    .Select(static reference => reference.Name)
                    .Where(name => IsForbiddenCrossModuleReference(name, currentName))
                    .OfType<string>(),
            ];

            Assert.True(
                referencedRuthModules.Length == 0,
                $"{currentName} directly references internal module assemblies: {string.Join(", ", referencedRuthModules)}"
            );
        }
    }

    [Fact]
    public void ModuleContracts_Should_Only_Depend_On_SharedKernel()
    {
        var contractsAssemblies = GetModuleContractsAssemblies();
        Assert.NotEmpty(contractsAssemblies);

        string[] forbidden =
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.Identity",
            "Microsoft.FluentUI",
            "Npgsql",
            "StackExchange.Redis",
            "Ruth.Host",
            "Ruth.Persistence",
            .. _moduleInternalNamespaces,
        ];

        foreach (var contractsAssembly in contractsAssemblies)
        {
            var result = Types
                .InAssembly(contractsAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                FormatFailingTypes(
                    $"{contractsAssembly.GetName().Name} has forbidden dependencies",
                    result
                )
            );
        }
    }

    [Fact]
    public void Host_Components_Should_Not_Depend_On_Module_Internals()
    {
        var result = Types
            .InAssembly(HostAssembly.Instance)
            .That()
            .ResideInNamespaceStartingWith("Ruth.Host.Components")
            .ShouldNot()
            .HaveDependencyOnAny(_moduleInternalNamespaces)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailingTypes(
                "Ruth.Host.Components must only depend on Contracts and SharedKernel",
                result
            )
        );
    }

    [Fact]
    public void Handlers_Requests_And_Responses_Should_Be_Sealed()
    {
        Assembly[] assemblies =
        [
            SharedKernelAssembly.Instance,
            PersistenceAssembly.Instance,
            HostAssembly.Instance,
            .. GetModuleContractsAssemblies(),
            .. GetModuleAssemblies(),
        ];

        foreach (var assembly in assemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Handler", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Request", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Response", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Command", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Query", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Consumer", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("Validator", StringComparison.Ordinal)
                .Or()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .And()
                .HaveNameEndingWith("IntegrationEvent", StringComparison.Ordinal)
                .Should()
                .BeSealed()
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                FormatFailingTypes(
                    $"{assembly.GetName().Name} contains non-sealed Handler/Request/Response/Command/Query/Consumer/Validator/IntegrationEvent types",
                    result
                )
            );
        }
    }

    [Fact]
    public void Persistence_Should_Not_Depend_On_Web_Host_Or_Modules()
    {
        var result = Types
            .InAssembly(PersistenceAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.FluentUI",
                "StackExchange.Redis",
                "Ruth.Host",
                "Ruth.Modules",
                "Ruth.Providers"
            )
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailingTypes("Ruth.Persistence has forbidden dependencies", result)
        );
    }

    private static bool IsForbiddenCrossModuleReference(string? name, string currentModuleName)
    {
        return name is not null
            && name.StartsWith("Ruth.Modules.", StringComparison.Ordinal)
            && !string.Equals(name, currentModuleName, StringComparison.Ordinal)
            && !string.Equals(name, $"{currentModuleName}.Contracts", StringComparison.Ordinal)
            && !name.EndsWith(".Contracts", StringComparison.Ordinal);
    }

    private static string FormatFailingTypes(string prefix, TestResult result)
    {
        var names = result.FailingTypeNames ?? [];
        return $"{prefix}: {string.Join(", ", names)}";
    }
}
