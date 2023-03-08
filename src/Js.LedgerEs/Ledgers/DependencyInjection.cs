using MediatR;

namespace Js.LedgerEs.Ledgers;

public static class LedgersDependencyInjection
{
    public static void MapLedgerRoutes(this IEndpointRouteBuilder api)
    {
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
    }

    public static void MapLedgerHubs(this IEndpointRouteBuilder api)
    {
        api.MapHub<LedgerNotificationHub>("/ledger");
    }
}
