using Js.LedgerEs;
using Js.LedgerEs.Configuration;
using Js.LedgerEs.Dashboard;
using Js.LedgerEs.ErrorHandling;
using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;
using Js.LedgerEs.ReadModelPersistence;
using Js.LedgerEs.Validation;

using Microsoft.OpenApi.Models;

const string CORS_POLICY_NAME = "DevPolicy";

var assembly = typeof(Program).Assembly;
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
        cfg.RegisterServicesFromAssembly(assembly);
    })
    .AddEventSourcing(builder.Configuration, assembly)
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

builder.Services.AddSignalR()
    .AddJsonProtocol(opt =>
    {
        opt.PayloadSerializerOptions = JsonConfig.SerializerOptions;
    });

builder.Logging
    .ClearProviders()
    .AddConsole();

var app = builder.Build();

app
    .UseErrorHandling()
    .UseValidation()
    .UseCors(CORS_POLICY_NAME)
    .UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger ES V1");
        c.RoutePrefix = "";
    });

var api = app.MapGroup("/api");
api.MapDashboardRoutes();
api.MapLedgerRoutes();

var signalr = app.MapGroup("/signalr");
signalr.MapDashboardHubs();
signalr.MapLedgerHubs();

app.Run();
