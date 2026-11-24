using System.Collections.Concurrent;
using System.Reflection;
using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Safe allow-listed registry mapping Outbox EventType strings to <see cref="IIntegrationEvent" /> CLR types.
/// </summary>
public sealed class OutboxEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _nameToType = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();

    /// <summary>Computes the canonical event type identifier for an integration event type.</summary>
    public static string GetCanonicalTypeName(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        var assemblyName = eventType.Assembly.GetName().Name;
        var fullName = eventType.FullName ?? eventType.Name;
        return string.IsNullOrWhiteSpace(assemblyName) ? fullName : $"{fullName}, {assemblyName}";
    }

    /// <summary>Explicitly registers an integration event type under an optional logical key.</summary>
    public OutboxEventTypeRegistry Register<TEvent>(string? eventTypeName = null)
        where TEvent : IIntegrationEvent
    {
        return Register(typeof(TEvent), eventTypeName);
    }

    /// <summary>Explicitly registers an integration event type under an optional logical key.</summary>
    public OutboxEventTypeRegistry Register(Type eventType, string? eventTypeName = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        if (!IsValidIntegrationEventType(eventType))
        {
            throw new ArgumentException(
                $"Type '{eventType.FullName}' must be a concrete IIntegrationEvent implementation.",
                nameof(eventType)
            );
        }

        var key = string.IsNullOrWhiteSpace(eventTypeName)
            ? GetCanonicalTypeName(eventType)
            : eventTypeName;

        if (_nameToType.TryGetValue(key, out var existing) && existing != eventType)
        {
            throw new InvalidOperationException(
                $"Outbox event key '{key}' is already registered for '{existing.FullName}'."
            );
        }

        _nameToType[key] = eventType;
        _typeToName[eventType] = key;
        return this;
    }

    /// <summary>Registers all concrete <see cref="IIntegrationEvent" /> implementations in <paramref name="assembly" />.</summary>
    public OutboxEventTypeRegistry RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (IsValidIntegrationEventType(type) && !_typeToName.ContainsKey(type))
            {
                _ = Register(type);
            }
        }

        return this;
    }

    /// <summary>Gets the registered or canonical event type string for <paramref name="eventType" />.</summary>
    public string GetEventTypeName(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return _typeToName.TryGetValue(eventType, out var registered)
            ? registered
            : GetCanonicalTypeName(eventType);
    }

    /// <summary>
    /// Resolves a stored event type name to a concrete <see cref="IIntegrationEvent" /> CLR type,
    /// returning <see langword="null" /> if unknown or disallowed.
    /// </summary>
    public Type? Resolve(string eventTypeName)
    {
        if (string.IsNullOrWhiteSpace(eventTypeName))
        {
            return null;
        }

        if (_nameToType.TryGetValue(eventTypeName, out var registered))
        {
            return registered;
        }

        var resolved = TryGetTypeSafely(eventTypeName);
        return IsAllowedFallbackType(resolved)
            ? resolved
            : FindInLoadedRuthAssemblies(eventTypeName);
    }

    private static Type? FindInLoadedRuthAssemblies(string eventTypeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is null || !name.StartsWith("Ruth.", StringComparison.Ordinal))
            {
                continue;
            }

            var candidate = TryGetAssemblyTypeSafely(assembly, eventTypeName);
            if (IsAllowedFallbackType(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Type? TryGetTypeSafely(string eventTypeName)
    {
        try
        {
            return Type.GetType(eventTypeName, throwOnError: false);
        }
        catch (Exception ex)
            when (ex
                    is FileLoadException
                        or FileNotFoundException
                        or BadImageFormatException
                        or TypeLoadException
                        or ArgumentException
            )
        {
            return null;
        }
    }

    private static Type? TryGetAssemblyTypeSafely(Assembly assembly, string eventTypeName)
    {
        try
        {
            return assembly.GetType(eventTypeName, throwOnError: false);
        }
        catch (Exception ex)
            when (ex
                    is FileLoadException
                        or FileNotFoundException
                        or BadImageFormatException
                        or TypeLoadException
                        or ArgumentException
            )
        {
            return null;
        }
    }

    private static bool IsValidIntegrationEventType(Type type)
    {
        return !type.IsAbstract
            && !type.IsInterface
            && !type.ContainsGenericParameters
            && typeof(IIntegrationEvent).IsAssignableFrom(type);
    }

    private static bool IsAllowedFallbackType(Type? type)
    {
        if (type is null || !IsValidIntegrationEventType(type))
        {
            return false;
        }

        var assemblyName = type.Assembly.GetName().Name;
        return assemblyName is not null
            && assemblyName.StartsWith("Ruth.", StringComparison.Ordinal);
    }
}
