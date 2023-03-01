namespace Js.LedgerEs
{
    public interface IAggregate
    {
        public void Apply(object? @event);
    }
}
