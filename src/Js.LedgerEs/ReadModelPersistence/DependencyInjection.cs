using Js.LedgerEs.Commands;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.ReadModelPersistence;

public static class ReadModelPersistenceDependencyInjection
{
    public static IServiceCollection AddReadModelPersistence(this IServiceCollection services)
        => services
            .AddTransient<IProjectionRevisionRepository, ProjectionRevisionRepository>()

            // Ledger Read Model
            .AddTransient<IReadModelUpdater, LedgerReadModelUpdater>()
            .AddProjection<LedgerOpened, LedgerReadModelUpdater>()
            .AddProjection<ReceiptJournalled, LedgerReadModelUpdater>()
            .AddProjection<PaymentJournalled, LedgerReadModelUpdater>()
            .AddProjection<LedgerClosed, LedgerReadModelUpdater>()

            // Dashboard Read Model
            .AddTransient<IReadModelUpdater, DashboardReadModelUpdater>()
            .AddProjection<LedgerOpened, DashboardReadModelUpdater>()
            .AddProjection<ReceiptJournalled, DashboardReadModelUpdater>()
            .AddProjection<PaymentJournalled, DashboardReadModelUpdater>()
            .AddProjection<LedgerClosed, DashboardReadModelUpdater>()

            .AddTransient<ISubscriptionHandler, SubscriptionHandler>()
            .AddHostedService<SubscriptionHandlerHostedService>();

    internal static IServiceCollection AddProjection<TEvent, TReadModelUpdater>(this IServiceCollection services)
            where TEvent : class, ISerializableEvent
            where TReadModelUpdater : class, IReadModelUpdater
        => services.AddSingleton<IReadModelUpdaterEventHandlerRegistration>(new ReadModelUpdaterEventHandlerRegistration<TReadModelUpdater, TEvent>());
}
