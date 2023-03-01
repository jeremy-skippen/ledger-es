using EventStore.Client;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Js.LedgerEs.OpenLedger;

public class OpenLedgerHandler : IRequestHandler<OpenLedger, LedgerOpened>
{
    private readonly EventStoreClient _eventStore;

    public OpenLedgerHandler(EventStoreClient eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<LedgerOpened> Handle(OpenLedger request, CancellationToken cancellationToken)
    {
        var streamName = LedgerAggregate.GetStreamName(request.LedgerId);
        var ledger = await _eventStore.AggregateStream<LedgerAggregate>(streamName, cancellationToken);
        if (ledger?.Open ?? false)
            throw new ValidationException(
                $"Ledger {request.LedgerId} is already open",
                new[]
                {
                    new ValidationFailure(nameof(request.LedgerId), $"Ledger is already open with id '{request.LedgerId}'")
                }
            );

        var @event = new LedgerOpened(request.LedgerId, request.LedgerName, DateTimeOffset.Now)
        {
            EventId = Uuid.NewUuid(),
        };
        var eventData = @event.SerializeToEventData();

        await _eventStore.AppendToStreamAsync(streamName, StreamState.Any, new[] { eventData }, cancellationToken: cancellationToken);

        return @event;
    }
}
