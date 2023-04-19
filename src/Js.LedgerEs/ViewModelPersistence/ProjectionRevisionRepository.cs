using Dapper;

using Js.LedgerEs.Configuration;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.ViewModelPersistence;

/// <summary>
/// Repository used to store the position in the "all" stream for a projection.
/// </summary>
public interface IProjectionRevisionRepository
{
    /// <summary>
    /// Get the stream position for a projection.
    /// </summary>
    /// <param name="projectionName">
    /// The name of the projection.
    /// </param>
    /// <returns>
    /// The stream position.
    /// </returns>
    Task<ulong> GetStreamPosition(string projectionName);

    /// <summary>
    /// Set the stream position for a projection.
    /// </summary>
    /// <param name="projectionName">
    /// The name of the projection.
    /// </param>
    /// <param name="position">
    /// The stream position.
    /// </param>
    Task SetStreamPosition(string projectionName, ulong position);
}

public class ProjectionRevisionRepository : IProjectionRevisionRepository
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;

    private const string GET_QUERY = @"
        SELECT StreamPosition
        FROM dbo.ProjectionPosition
        WHERE ProjectionName = @projectionName;
    ";
    private const string SET_QUERY = @"
        UPDATE TOP (1) dbo.ProjectionPosition WITH (UPDLOCK, SERIALIZABLE)
        SET StreamPosition = @streamPosition
        WHERE ProjectionName = @projectionName;

        IF (@@ROWCOUNT = 0)
        BEGIN
            INSERT INTO dbo.ProjectionPosition(ProjectionName, StreamPosition)
            VALUES (@projectionName, @streamPosition);
        END
    ";

    public ProjectionRevisionRepository(IOptions<LedgerEsConfiguration> cfg)
    {
        _cfg = cfg;
    }

    public async Task<ulong> GetStreamPosition(string projectionName)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        return await conn.ExecuteScalarAsync<ulong>(GET_QUERY, new { projectionName });
    }

    public async Task SetStreamPosition(string projectionName, ulong position)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        await conn.ExecuteAsync(SET_QUERY, new { projectionName, streamPosition = (long)position });
    }
}
