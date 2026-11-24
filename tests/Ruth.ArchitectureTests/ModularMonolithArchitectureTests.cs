using System.Reflection;
using NetArchTest.Rules;
using Ruth.Host;
using Ruth.SharedKernel;
using Xunit;

namespace Ruth.ArchitectureTests;

public sealed class ModularMonolithArchitectureTests
{
    private static Assembly[] GetModuleAssemblies()
    {
        return
        [
            .. AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(static assembly => IsModuleImplementationAssembly(assembly.GetName().Name)),
        ];
    }

    private static bool IsModuleImplementationAssembly(string? name)
    {
        return name is not null
            && name.StartsWith("Ruth.Modules.", StringComparison.Ordinal)
            && !name.EndsWith(".Contracts", StringComparison.Ordinal)
            && !name.EndsWith(".Tests", StringComparison.Ordinal);
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
        foreach (var moduleAssembly in GetModuleAssemblies())
        {
            var result = Types
                .InAssembly(moduleAssembly)
                .That()
                .ResideInNamespaceContaining(".Domain")
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.AspNetCore",
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.FluentUI",
                    "Npgsql",
                    "StackExchange.Redis"
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
        foreach (var moduleAssembly in GetModuleAssemblies())
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
    public void Handlers_Requests_And_Responses_Should_Be_Sealed()
    {
        Assembly[] assemblies =
        [
            SharedKernelAssembly.Instance,
            HostAssembly.Instance,
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
                .Should()
                .BeSealed()
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                FormatFailingTypes(
                    $"{assembly.GetName().Name} contains non-sealed Handler/Request/Response types",
                    result
                )
            );
        }
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
