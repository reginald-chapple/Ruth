using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Ruth.Host.Components.Layout;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal static class BlazorTestDoubles
{
    internal sealed class ErrorControl
    {
        public bool ShouldThrow { get; set; }
    }

    internal sealed class MainLayoutTestHost : ComponentBase
    {
        [Parameter]
        public RenderFragment? Body { get; set; }

        [Parameter]
        public Action<MainLayout>? OnLayoutCaptured { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MainLayout>(0);
            builder.AddAttribute(1, nameof(LayoutComponentBase.Body), Body);
            builder.AddComponentReferenceCapture(2, component =>
                OnLayoutCaptured?.Invoke((MainLayout)component)
            );
            builder.CloseComponent();
        }
    }

    internal sealed class ThrowingChildComponent : ComponentBase
    {
        [Parameter]
        public ErrorControl Control { get; set; } = new();

        public void TriggerRender()
        {
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Control.ShouldThrow)
            {
                throw new InvalidOperationException("secret-render-boom");
            }

            builder.AddContent(0, "Recovered normal content");
        }
    }

    internal sealed class RecordingJsRuntime : IJSRuntime
    {
        public List<string> InvokedIdentifiers { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            InvokedIdentifiers.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        )
        {
            InvokedIdentifiers.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    internal sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        public List<string> NavigatedUris { get; } = [];

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedUris.Add(uri);
        }
    }

    internal sealed class TestErrorBoundaryLogger : IErrorBoundaryLogger
    {
        public ValueTask LogErrorAsync(Exception exception)
        {
            return ValueTask.CompletedTask;
        }
    }
}
