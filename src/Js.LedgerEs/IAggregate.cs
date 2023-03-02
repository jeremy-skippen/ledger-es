namespace Js.LedgerEs
{
    public interface IAggregate
    {
        ulong Version { get; }

        void Apply(object? @event);
    }
}
