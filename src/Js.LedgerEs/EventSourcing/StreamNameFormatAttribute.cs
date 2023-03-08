namespace Js.LedgerEs.EventSourcing;

[AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited = false)]
public sealed class StreamNameFormatAttribute : Attribute
{
    public string Format { get; }

    public StreamNameFormatAttribute(string format)
    {
        Format = format;
    }
}
