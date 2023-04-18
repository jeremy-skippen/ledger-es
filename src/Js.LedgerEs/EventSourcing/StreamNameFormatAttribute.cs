namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// This attribute is used to annotate write models (<see cref="IWriteModel"/>) with the format of the stream name
/// to use in the event store.
/// </summary>
/// <example>
/// [StreamNameFormat("write-model-{0:d}")]
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StreamNameFormatAttribute : Attribute
{
    /// <summary>
    /// The string format of the stream name. The format must include 1 positional Guid parameter.
    /// </summary>
    public string Format { get; }

    public StreamNameFormatAttribute(string format)
    {
        Format = format;
    }
}
