using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn;
using Ruth.Modules.Cms;
using Ruth.Modules.Commerce;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Crm;
using Ruth.Modules.Engagement;
using Ruth.Modules.Events;
using Ruth.Modules.Finance;
using Ruth.Modules.Groups;
using Ruth.Modules.Identity;
using Ruth.Modules.Lms;
using Ruth.Modules.Reporting;
using Ruth.Modules.Search;
using Ruth.Modules.Tenancy;
using Ruth.Modules.Workflow;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.ArchitectureTests;

public sealed class TenantIsolationArchitectureTests
{
    private const string DummyConnectionString =
        "Host=localhost;Port=5432;Database=ruth_arch_check;Username=ruth;Password=ruth";

    private static readonly MethodInfo _instantiateMethod =
        typeof(TenantIsolationArchitectureTests).GetMethod(
            nameof(InstantiateDbContext),
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

    private static readonly PropertyInfo _isTenantScopedProp = typeof(ModuleDbContext).GetProperty(
        "IsTenantScopedSchema",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
    )!;

    [Fact]
    public void All_Fifteen_Tenant_ModuleDbContexts_Enforce_ITenantScoped_TenantId_Column_And_QueryFilter()
    {
        var dbContextTypes = GetAllModuleDbContextTypes();
        Assert.Equal(16, dbContextTypes.Count);

        var tenantCount = 0;
        var platformCount = 0;

        foreach (var ctxType in dbContextTypes)
        {
            using var context = (ModuleDbContext)
                _instantiateMethod.MakeGenericMethod(ctxType).Invoke(obj: null, parameters: null)!;
            var isTenantScoped = (bool)_isTenantScopedProp.GetValue(context)!;

            if (string.Equals(ctxType.Name, "TenancyDbContext", StringComparison.Ordinal))
            {
                platformCount++;
                Assert.False(isTenantScoped);
                Assert.Equal("platform", context.Model.GetDefaultSchema());
                continue;
            }

            tenantCount++;
            AssertTenantDbContextModel(context, isTenantScoped);
        }

        Assert.Equal(15, tenantCount);
        Assert.Equal(1, platformCount);
    }

    [Fact]
    public void Removed_Video_WalletPasses_And_ApacheAge_Artifacts_Do_Not_Exist_In_Solution()
    {
        string[] forbiddenTokens = ["VirtualRooms", "LiveKit", "WalletPass", "ApacheAge"];
        var ruthAssemblies = GetModuleAssemblies()
            .Concat(
                AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(static a =>
                        a.GetName().Name?.StartsWith("Ruth.", StringComparison.Ordinal) == true
                        && !string.Equals(
                            a.GetName().Name,
                            "Ruth.ArchitectureTests",
                            StringComparison.Ordinal
                        )
                    )
            )
            .Distinct()
            .ToArray();

        foreach (var assembly in ruthAssemblies)
        {
            AssertAssemblyFreeOfForbiddenTokens(assembly, forbiddenTokens);
        }
    }

    private static List<Type> GetAllModuleDbContextTypes()
    {
        return
        [
            .. GetModuleAssemblies()
                .SelectMany(static a => a.GetTypes())
                .Where(static t => !t.IsAbstract && typeof(ModuleDbContext).IsAssignableFrom(t))
                .OrderBy(static t => t.FullName, StringComparer.Ordinal),
        ];
    }

    private static Assembly[] GetModuleAssemblies()
    {
        return
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
            EngagementModuleAssembly.Instance,
            WorkflowModuleAssembly.Instance,
            CmsModuleAssembly.Instance,
            ReportingModuleAssembly.Instance,
            LmsModuleAssembly.Instance,
            SearchModuleAssembly.Instance,
            TenancyModuleAssembly.Instance,
        ];
    }

    private static void AssertTenantDbContextModel(ModuleDbContext context, bool isTenantScoped)
    {
        var contextName = context.GetType().Name;
        Assert.True(isTenantScoped, $"{contextName} must have IsTenantScopedSchema = true.");

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            Assert.True(
                typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType),
                $"{contextName} entity '{entityType.ClrType.FullName}' must implement ITenantScoped."
            );

            var tenantProp = entityType.FindProperty(nameof(ITenantScoped.TenantId));
            Assert.NotNull(tenantProp);
            Assert.Equal("tenant_id", tenantProp.GetColumnName());

            Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
        }
    }

    private static void AssertAssemblyFreeOfForbiddenTokens(
        Assembly assembly,
        string[] forbiddenTokens
    )
    {
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        foreach (var type in assembly.GetTypes())
        {
            var fullName = type.FullName ?? type.Name;
            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, fullName, StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var referenced in assembly.GetReferencedAssemblies())
        {
            var refName = referenced.Name ?? string.Empty;
            foreach (var token in forbiddenTokens)
            {
                Assert.False(
                    refName.Contains(token, StringComparison.OrdinalIgnoreCase),
                    $"{assemblyName} references forbidden assembly {refName}."
                );
            }
        }
    }

    private static TContext InstantiateDbContext<TContext>()
        where TContext : ModuleDbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(DummyConnectionString, static npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return (TContext)
            Activator.CreateInstance(
                typeof(TContext),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                args: [options],
                culture: null
            )!;
    }
}
