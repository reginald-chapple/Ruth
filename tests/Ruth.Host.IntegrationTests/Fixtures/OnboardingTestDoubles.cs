using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ruth.Host.Components.Layout;
using Ruth.Host.Components.Pages;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal static class OnboardingTestDoubles
{
    internal sealed class OnboardingTestHost : ComponentBase
    {
        [Parameter]
        public Action<Onboarding>? OnPageCaptured { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Onboarding>(0);
            builder.AddComponentReferenceCapture(1, component =>
                OnPageCaptured?.Invoke((Onboarding)component)
            );
            builder.CloseComponent();
        }
    }

    internal sealed class OnboardingLayoutTestHost : ComponentBase
    {
        [Parameter]
        public RenderFragment? Body { get; set; }

        [Parameter]
        public Action<OnboardingLayout>? OnLayoutCaptured { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<OnboardingLayout>(0);
            builder.AddAttribute(1, nameof(LayoutComponentBase.Body), Body);
            builder.AddComponentReferenceCapture(2, component =>
                OnLayoutCaptured?.Invoke((OnboardingLayout)component)
            );
            builder.CloseComponent();
        }
    }

    internal sealed class RecordingCommandDispatcher(Func<object, object>? responder = null)
        : ICommandDispatcher
    {
        public List<object> SentCommands { get; } = [];

        public Task<Result> SendAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default
        )
            where TCommand : ICommand
        {
            SentCommands.Add(command);
            return Task.FromResult(Result.Success());
        }

        public Task<Result<TResponse>> SendAsync<TCommand, TResponse>(
            TCommand command,
            CancellationToken cancellationToken = default
        )
            where TCommand : ICommand<TResponse>
        {
            SentCommands.Add(command);
            return responder is not null
                ? Task.FromResult((Result<TResponse>)responder(command))
                : throw new InvalidOperationException(
                    "No responder configured for RecordingCommandDispatcher."
                );
        }
    }

    internal static void AssertTimeZoneDropdownOmitsRequired(string html)
    {
        var idIndex = html.IndexOf("id=\"org-timezone\"", StringComparison.OrdinalIgnoreCase);
        Assert.True(idIndex >= 0, "Expected rendered HTML to contain id=\"org-timezone\".");
        var tagStart = html.LastIndexOf('<', idIndex);
        var tagEnd = html.IndexOf('>', idIndex);
        Assert.True(tagStart >= 0 && tagEnd > tagStart);
        var openTag = html[tagStart..(tagEnd + 1)];
        Assert.StartsWith("<fluent-dropdown", openTag, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("required", openTag, StringComparison.OrdinalIgnoreCase);
    }
}
