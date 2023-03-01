using EventStore.Client;

using Js.LedgerEs;
using Js.LedgerEs.OpenLedger;
using Js.LedgerEs.Validation;

using MediatR;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Configuration
    .AddEnvironmentVariables();

builder.Services
    .AddLogging()
    .AddValidators()
    .AddMediatR(cfg =>
    {
        cfg.AddValidationBehavior();
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    })
    .AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("EventStore"))))
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
    .UseSwagger();

if (isDevelopment)
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger ES V1");
        c.RoutePrefix = "";
    });
}

app.MapPost("/api/ledger", async (OpenLedger request, IMediator mediator, CancellationToken ct) => await mediator.Send(request, ct));

app.Run();