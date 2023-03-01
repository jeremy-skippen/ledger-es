using Js.LedgerEs.OpenLedger;

namespace Js.LedgerEs
{
    public sealed class LedgerAggregate :
        IAggregate,
        IEventHandler<LedgerOpened>
    {
        public string LedgerId { get; private set; }

        public string LedgerName { get; private set; }

        public int Version { get; private set; }

        public bool Open { get; private set; }

        public static string GetStreamName(string ledgerId) => $"ledger-{ledgerId.ToLowerInvariant()}";

        public LedgerAggregate()
        {
            LedgerId = string.Empty;
            LedgerName = string.Empty;
            Version = 0;
            Open = false;
        }

        public void Apply(object? @event)
        {
            switch (@event)
            {
                case LedgerOpened opened:
                    Handle(opened);
                    break;
            };
        }

        public void Handle(LedgerOpened @event)
        {
            if (Open)
                throw new InvalidStateChangeException();

            LedgerId = @event.LedgerId;
            LedgerName = @event.LedgerName;
            Version += 1;
            Open = true;
        }
    }
}
