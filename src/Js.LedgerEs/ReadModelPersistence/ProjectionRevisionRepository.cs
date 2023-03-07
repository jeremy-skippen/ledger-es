using Dapper;

using EventStore.Client;

using Js.LedgerEs.Configuration;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.ReadModelPersistence;

public interface IProjectionRevisionRepository
{
    Task<Position?> GetStreamPosition(string projectionName);

    Task SetStreamPosition(string projectionName, Position position);
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

    public async Task<Position?> GetStreamPosition(string projectionName)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        var rawPosition = await conn.ExecuteScalarAsync<ulong>(GET_QUERY, new { projectionName });
        if (rawPosition == 0)
            return null;

        return new Position(rawPosition, rawPosition);
    }

    public async Task SetStreamPosition(string projectionName, Position position)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        await conn.ExecuteAsync(SET_QUERY, new { projectionName, streamPosition = (long)position.CommitPosition });
    }
}
