using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Infrastructure.Templating;

internal sealed class RazorHtmlTemplateRenderer(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    IRazorTemplateRegistry registry
) : IRazorTemplateRenderer
{
    public Task<Result<string>> RenderComponentAsync<TComponent>(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        return RenderComponentAsync(typeof(TComponent), parameters, cancellationToken);
    }

    public async Task<Result<string>> RenderComponentAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(componentType);
        cancellationToken.ThrowIfCancellationRequested();

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            return CommunicationTemplateErrors.InvalidRazorComponentType(componentType);
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var htmlRenderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);

            var parameterView = BuildParameterView(parameters);
            var rendered = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var root = await htmlRenderer.RenderComponentAsync(componentType, parameterView);
                return root.ToHtmlString();
            });

            return Result.Success(rendered);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return CommunicationTemplateErrors.RazorRenderFailed(componentType.Name, ex.Message);
        }
    }

    public Task<Result<string>> RenderNamedTemplateAsync(
        string templateKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var descriptor = registry.FindByKey(templateKey);
        return descriptor is null
            ? Task.FromResult(
                Result.Failure<string>(
                    CommunicationTemplateErrors.RazorTemplateNotFound(templateKey)
                )
            )
            : RenderComponentAsync(descriptor.ComponentType, parameters, cancellationToken);
    }

    private static ParameterView BuildParameterView(
        IReadOnlyDictionary<string, object?>? parameters
    )
    {
        if (parameters is null || parameters.Count == 0)
        {
            return ParameterView.Empty;
        }

        var copy = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);
        foreach (var pair in parameters)
        {
            copy[pair.Key] = pair.Value;
        }

        return ParameterView.FromDictionary(copy);
    }
}
