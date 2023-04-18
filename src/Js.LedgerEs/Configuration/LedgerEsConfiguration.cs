namespace Js.LedgerEs.Configuration;

/// <summary>
/// Contains the application-specific configuration values.
/// </summary>
public sealed class LedgerEsConfiguration
{
    /// <summary>
    /// The SQL Server connection string used to store the application read-models / projections.
    /// </summary>
    public string SqlServerConnectionString { get; set; } = "";
}
