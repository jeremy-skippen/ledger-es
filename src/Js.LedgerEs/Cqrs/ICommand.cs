using MediatR;

namespace Js.LedgerEs.Cqrs;

/// <summary>
/// Represents a command that, if successfully processed, will generate an event that should be serialized to the
/// event stream.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Get the unique identifier of the stream this command is for.
    /// </summary>
    /// <returns>
    /// The unique identifier of the stream this command is for.
    /// </returns>
    Guid GetStreamUniqueIdentifier();
}
