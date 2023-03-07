using EventStore.Client;

using Js.LedgerEs;
using Js.LedgerEs.Commands;
using Js.LedgerEs.Configuration;
using Js.LedgerEs.ErrorHandling;
using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Notification;
using Js.LedgerEs.ReadModelPersistence;
using Js.LedgerEs.Requests;
using Js.LedgerEs.Validation;

using MediatR;

using Microsoft.OpenApi.Models;

const string CORS_POLICY_NAME = "DevPolicy";

var builder = WebApplication.CreateBuilder(args);

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
    .AddCors(opt =>
    {
        opt.AddPolicy(CORS_POLICY_NAME, builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    })
    .AddProblemDetails()
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

builder.Services.AddSignalR();

builder.Logging
    .ClearProviders()
    .AddConsole();

var app = builder.Build();

app
    .UseEventSerialization()
    .UseErrorHandling()
    .UseValidation()
    .UseCors()
    .UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger ES V1");
        c.RoutePrefix = "";
    });

var api = app
    .MapGroup("/api")
    .RequireCors(CORS_POLICY_NAME);

api.MapGet(
    "/dashboard",
    async (IMediator mediator, CancellationToken ct)
        => await mediator.Send(new GetDashboard(), ct)
);

api.MapGet(
    "/ledger",
    async (int? page, int? pageSize, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new GetLedgerList(page, pageSize), ct)
);

api.MapPost(
    "/ledger",
    async (OpenLedger request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(request, ct)
);

api.MapGet(
    "/ledger/{ledgerId:guid}",
    async (Guid ledgerId, IMediator mediator, CancellationToken ct) =>
    {
        var response = await mediator.Send(new GetLedgerRawJson(ledgerId), ct);
        return response.Ledger is null
            ? Results.NotFound()
            : Results.Text(response.Ledger, "application/json");
    }
);

api.MapPost(
    "/ledger/{ledgerId:guid}/receipt",
    async (Guid ledgerId, JournalReceiptRequestBody request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new JournalReceipt(ledgerId, request.Description, request.Amount), ct)
);

api.MapPost(
    "/ledger/{ledgerId:guid}/payment",
    async (Guid ledgerId, JournalPaymentRequestBody request, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new JournalPayment(ledgerId, request.Description, request.Amount), ct)
);

api.MapDelete(
    "/ledger/{ledgerId:guid}",
    async (Guid ledgerId, IMediator mediator, CancellationToken ct)
        => await mediator.Send(new CloseLedger(ledgerId), ct)
);

var signalr = app
    .MapGroup("/signalr")
    .RequireCors(CORS_POLICY_NAME);

signalr.MapHub<DashboardNotificationHub>("/dashboard");

app.Run();
