using EventStore.Client;

using Js.LedgerEs;
using Js.LedgerEs.Commands;
using Js.LedgerEs.Configuration;
using Js.LedgerEs.ErrorHandling;
using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.ReadModelPersistence;
using Js.LedgerEs.Requests;
using Js.LedgerEs.Validation;

using MediatR;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Configuration
    .AddEnvironmentVariables();

builder.Services
    .Configure<LedgerEsConfiguration>(cfg =>
    {
        cfg.SqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServer") ?? throw new Exception("SqlServer connection string is missing");
    })
    .AddLogging()
    .AddAutoMapper(typeof(MappingProfile))
    .AddValidators()
    .AddMediatR(cfg =>
    {
        cfg.AddValidationBehavior();
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    })
    .AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("EventStore"))))
    .AddReadModelPersistence()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Ledger ES",
            Version = "v1",
        });
    })
    ;

builder.Logging
    .ClearProviders()
    .AddConsole();

var app = builder.Build();

app
    .UseEventSerialization()
    .UseValidation()
    .UseMiddleware<ApplicationExceptionHandlingMiddleware>()
    .UseSwagger();

if (isDevelopment)
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger ES V1");
        c.RoutePrefix = "";
    });
}

app.MapGet(
    "/api/dashboard",
    async (IMediator mediator, CancellationToken ct)
        => await mediator.Send(new GetDashboard(), ct)
);

app.MapGet(
    "/api/ledger",
    async (int? page, int? pageSize, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new GetLedgerList(page, pageSize), ct)
);

app.MapPost(
    "/api/ledger",
    async (OpenLedger request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(request, ct)
);

app.MapGet(
    "/api/ledger/{ledgerId:guid}",
    async (Guid ledgerId, IMediator mediator, CancellationToken ct) =>
    {
        var response = await mediator.Send(new GetLedgerRawJson(ledgerId), ct);
        return response.Ledger is null
            ? Results.NotFound()
            : Results.Text(response.Ledger, "application/json");
    }
);

app.MapPost(
    "/api/ledger/{ledgerId:guid}/receipt",
    async (Guid ledgerId, JournalReceiptRequestBody request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new JournalReceipt(ledgerId, request.Description, request.Amount), ct)
);

app.MapPost(
    "/api/ledger/{ledgerId:guid}/payment",
    async (Guid ledgerId, JournalPaymentRequestBody request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new JournalPayment(ledgerId, request.Description, request.Amount), ct)
);

app.MapDelete(
    "/api/ledger/{ledgerId:guid}",
    async (Guid ledgerId, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new CloseLedger(ledgerId), ct)
);

app.Run();
