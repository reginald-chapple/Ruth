using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Slices.Registrations;

internal static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var staffGroup = endpoints
            .MapGroup("/api/events/registrations")
            .WithTags("Events Registrations & Waitlists")
            .RequireAuthorization();

        MapTemplateAndInstanceRoutes(staffGroup);
        MapStaffOperationsRoutes(staffGroup);

        var publicGroup = endpoints
            .MapGroup("/api/events/registrations/public")
            .WithTags("Events Registrations & Waitlists")
            .AllowAnonymous();

        MapPublicWizardRoutes(publicGroup);
        return endpoints;
    }

    private static void MapTemplateAndInstanceRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/templates", ListTemplatesAsync)
            .WithName("EventsListRegistrationTemplates")
            .WithSummary("List registration templates")
            .WithDescription("Returns registration templates with forms, fees, and discounts.")
            .Produces<IReadOnlyList<RegistrationTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/templates/{id:guid}", GetTemplateByIdAsync)
            .WithName("EventsGetRegistrationTemplateById")
            .WithSummary("Get registration template by ID")
            .WithDescription("Returns a registration template and its nested configuration.")
            .Produces<RegistrationTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/templates", CreateTemplateAsync)
            .WithName("EventsCreateRegistrationTemplate")
            .WithSummary("Create registration template")
            .WithDescription(
                "Creates a template with dynamic forms, fees, discounts, and placements."
            )
            .Produces<RegistrationTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/templates/{id:guid}", UpdateTemplateAsync)
            .WithName("EventsUpdateRegistrationTemplate")
            .WithSummary("Update registration template")
            .WithDescription("Updates template forms, fees, discounts, and payment policies.")
            .Produces<RegistrationTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/instances", ListInstancesAsync)
            .WithName("EventsListRegistrationInstances")
            .WithSummary("List registration instances")
            .WithDescription("Returns registration instances with live seat and waitlist counts.")
            .Produces<IReadOnlyList<RegistrationInstanceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/instances", CreateInstanceAsync)
            .WithName("EventsCreateRegistrationInstance")
            .WithSummary("Create registration instance")
            .WithDescription("Creates a registration instance linked to a template.")
            .Produces<RegistrationInstanceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapStaffOperationsRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/instances/{instanceId:guid}/submissions", ListInstanceRegistrationsAsync)
            .WithName("EventsListRegistrationsByInstance")
            .WithSummary("List registrations for an instance")
            .WithDescription(
                "Returns registrations, registrants, waitlist positions, and balances."
            )
            .Produces<IReadOnlyList<RegistrationResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost(
                "/{registrationId:guid}/registrants/{registrantId:guid}/cancel",
                CancelRegistrantAsync
            )
            .WithName("EventsCancelRegistrant")
            .WithSummary("Cancel a registrant and auto-promote FIFO waitlist")
            .WithDescription(
                "Cancels a registrant and automatically promotes the next waitlisted attendee."
            )
            .Produces<CancelRegistrantResultResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost(
                "/{registrationId:guid}/registrants/{registrantId:guid}/place",
                PlaceRegistrantAsync
            )
            .WithName("EventsPlaceRegistrantInGroup")
            .WithSummary("Place a registrant into a Group")
            .WithDescription(
                "Places a confirmed registrant into a target Group via Groups.Contracts."
            )
            .Produces<RegistrationRegistrantResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/sessions/expire", ExpireSessionsAsync)
            .WithName("EventsExpireRegistrationSessions")
            .WithSummary("Expire stale seat-hold sessions and run waitlist auto-promotion")
            .WithDescription("Releases expired TTL seat holds and promotes waitlisted registrants.")
            .Produces<RegistrationSessionCleanupResultResponse>(StatusCodes.Status200OK);
    }

    private static void MapPublicWizardRoutes(RouteGroupBuilder publicGroup)
    {
        _ = publicGroup
            .MapGet("/{slugOrId}", GetWizardInitAsync)
            .WithName("EventsGetPublicRegistrationWizard")
            .WithSummary("Bootstrap public Registration Wizard by instance slug or ID")
            .WithDescription("Returns instance capacity and template forms, fees, and discounts.")
            .Produces<PublicRegistrationWizardInitResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = publicGroup
            .MapPost("/{slugOrId}/holds", AcquireHoldAsync)
            .WithName("EventsCreateRegistrationSessionHold")
            .WithSummary("Acquire a TTL seat-hold session")
            .WithDescription("Reserves confirmed or waitlist seats while completing the wizard.")
            .Produces<RegistrationSessionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = publicGroup
            .MapPost("/{slugOrId}/quote", PreviewPricingAsync)
            .WithName("EventsPreviewRegistrationPricing")
            .WithSummary("Preview registration pricing, fees, and discount code")
            .WithDescription("Evaluates seat split, fee options, discounts, and minimum deposit.")
            .Produces<RegistrationPricingQuoteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = publicGroup
            .MapPost("/{slugOrId}/submit", SubmitRegistrationAsync)
            .WithName("EventsSubmitRegistration")
            .WithSummary("Submit an event registration")
            .WithDescription(
                "Submits registrants, settles payment or invoice, and issues confirmation."
            )
            .Produces<RegistrationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = publicGroup
            .MapGet("/confirmations/{codeOrId}", GetConfirmationAsync)
            .WithName("EventsGetRegistrationConfirmation")
            .WithSummary("Lookup registration by confirmation code or ID")
            .WithDescription("Returns registration confirmation details and balance due.")
            .Produces<RegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = publicGroup
            .MapPost("/confirmations/{registrationId:guid}/payments", PayInstallmentAsync)
            .WithName("EventsRecordRegistrationPayment")
            .WithSummary("Pay an installment or remaining balance on a registration")
            .WithDescription(
                "Charges payment via Finance.Contracts and reduces registration balance."
            )
            .Produces<RegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RegistrationTemplateResponse>>, ProblemHttpResult>
    > ListTemplatesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationTemplatesQuery,
            IReadOnlyList<RegistrationTemplateResponse>
        >(new GetRegistrationTemplatesQuery(activeOnly ?? false), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationTemplateResponse>, ProblemHttpResult>
    > GetTemplateByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationTemplateByIdQuery,
            RegistrationTemplateResponse
        >(new GetRegistrationTemplateByIdQuery(id), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<RegistrationTemplateResponse>, ProblemHttpResult>
    > CreateTemplateAsync(
        RegistrationTemplateSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateRegistrationTemplateCommand,
            RegistrationTemplateResponse
        >(new CreateRegistrationTemplateCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/events/registrations/templates/{res.Value.Id:D}",
                res.Value
            )
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationTemplateResponse>, ProblemHttpResult>
    > UpdateTemplateAsync(
        Guid id,
        RegistrationTemplateSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateRegistrationTemplateCommand,
            RegistrationTemplateResponse
        >(new UpdateRegistrationTemplateCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RegistrationInstanceResponse>>, ProblemHttpResult>
    > ListInstancesAsync(
        Guid? templateId,
        Guid? branchId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationInstancesQuery,
            IReadOnlyList<RegistrationInstanceResponse>
        >(
            new GetRegistrationInstancesQuery(templateId, branchId, activeOnly ?? false),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<RegistrationInstanceResponse>, ProblemHttpResult>
    > CreateInstanceAsync(
        RegistrationInstanceSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateRegistrationInstanceCommand,
            RegistrationInstanceResponse
        >(new CreateRegistrationInstanceCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/events/registrations/instances/{res.Value.Id:D}",
                res.Value
            )
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RegistrationResponse>>, ProblemHttpResult>
    > ListInstanceRegistrationsAsync(
        Guid instanceId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationsByInstanceQuery,
            IReadOnlyList<RegistrationResponse>
        >(new GetRegistrationsByInstanceQuery(instanceId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<CancelRegistrantResultResponse>, ProblemHttpResult>
    > CancelRegistrantAsync(
        Guid registrationId,
        Guid registrantId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CancelRegistrantCommand,
            CancelRegistrantResultResponse
        >(new CancelRegistrantCommand(registrationId, registrantId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationRegistrantResponse>, ProblemHttpResult>
    > PlaceRegistrantAsync(
        Guid registrationId,
        Guid registrantId,
        PlaceRegistrantInGroupCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            PlaceRegistrantInGroupCommand,
            RegistrationRegistrantResponse
        >(
            body with
            {
                RegistrationId = registrationId,
                RegistrantId = registrantId,
            },
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationSessionCleanupResultResponse>, ProblemHttpResult>
    > ExpireSessionsAsync(
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            ExpireRegistrationSessionsCommand,
            RegistrationSessionCleanupResultResponse
        >(new ExpireRegistrationSessionsCommand(), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<PublicRegistrationWizardInitResponse>, ProblemHttpResult>
    > GetWizardInitAsync(
        string slugOrId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationInstancePortalQuery,
            PublicRegistrationWizardInitResponse
        >(new GetRegistrationInstancePortalQuery(slugOrId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<RegistrationSessionResponse>, ProblemHttpResult>
    > AcquireHoldAsync(
        string slugOrId,
        CreateRegistrationSessionHoldCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateRegistrationSessionHoldCommand,
            RegistrationSessionResponse
        >(body with { InstanceIdOrSlug = slugOrId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/events/registrations/public/{slugOrId}/holds", res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationPricingQuoteResponse>, ProblemHttpResult>
    > PreviewPricingAsync(
        string slugOrId,
        PreviewRegistrationPricingQuery body,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            PreviewRegistrationPricingQuery,
            RegistrationPricingQuoteResponse
        >(body with { InstanceIdOrSlug = slugOrId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<RegistrationResponse>, ProblemHttpResult>
    > SubmitRegistrationAsync(
        string slugOrId,
        SubmitRegistrationCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<SubmitRegistrationCommand, RegistrationResponse>(
            body with
            {
                InstanceIdOrSlug = slugOrId,
            },
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/events/registrations/public/confirmations/{res.Value.ConfirmationCode}",
                res.Value
            )
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationResponse>, ProblemHttpResult>
    > GetConfirmationAsync(
        string codeOrId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetRegistrationByConfirmationOrIdQuery,
            RegistrationResponse
        >(new GetRegistrationByConfirmationOrIdQuery(codeOrId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<RegistrationResponse>, ProblemHttpResult>
    > PayInstallmentAsync(
        Guid registrationId,
        RecordRegistrationPaymentCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            RecordRegistrationPaymentCommand,
            RegistrationResponse
        >(body with { RegistrationId = registrationId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
