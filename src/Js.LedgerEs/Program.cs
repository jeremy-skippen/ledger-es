using Js.LedgerEs.Configuration;
using Js.LedgerEs.Dashboard;
using Js.LedgerEs.ErrorHandling;
using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;
using Js.LedgerEs.ViewModelPersistence;
using Js.LedgerEs.Validation;

using Microsoft.OpenApi.Models;

#if DEBUG
const string CORS_POLICY_NAME = "DevPolicy";
#endif

var assembly = typeof(Program).Assembly;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddEnvironmentVariables();

builder.Services
    .Configure<LedgerEsConfiguration>(cfg =>
    {
        cfg.SqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServer") ?? throw new Exception("SqlServer connection string is missing");
    })
    .AddSingleton(JsonConfig.SerializerOptions)
    .AddLogging()
    .AddAutoMapper(assembly)
    .AddValidators()
    .AddMediatR(cfg =>
    {
        cfg.AddValidationBehavior();
        cfg.RegisterServicesFromAssembly(assembly);
    })
    .AddEventSourcing(builder.Configuration, assembly)
    .AddViewModelPersistence()
#if DEBUG
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
#endif
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

builder.Services
    .AddHealthChecks();

builder.Services
    .AddSignalR()
    .AddJsonProtocol(opt =>
    {
        opt.PayloadSerializerOptions = JsonConfig.SerializerOptions;
    });

builder.Logging
    .ClearProviders()
    .AddConsole();

var wapp = builder.Build();

wapp.UseErrorHandling()
    .UseValidation()
#if DEBUG
    .UseCors(CORS_POLICY_NAME)
#endif
    .UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger ES V1");
        c.RoutePrefix = "";
    });

wapp.MapHealthChecks("/health");

wapp.MapGroup("/api")
    .MapDashboardRoutes()
    .MapLedgerRoutes();

wapp.MapGroup("/signalr")
    .MapDashboardHubs()
    .MapLedgerHubs();

wapp.Run();
