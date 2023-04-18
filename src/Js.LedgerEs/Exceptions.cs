namespace Js.LedgerEs;

/// <summary>
/// Represents an application-level error.
/// </summary>
public class LedgerEsException : Exception
{
    /// <summary>
    /// Defines the HTTP response code to return if this exception is thrown and not caught.
    /// </summary>
    public virtual int HttpStatusCode => 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerEsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    public LedgerEsException(string? message) : base(message)
    {
    }
}
