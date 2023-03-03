using System.Data;

using Dapper;

using EventStore.Client;

using Microsoft.Data.SqlClient;

namespace Js.LedgerEs.ReadModelPersistence;

public interface IProjectionRevisionRepository
{
    Task<Position?> GetStreamPosition(SqlConnection conn, string projectionName);

    Task SetStreamPosition(SqlConnection conn, IDbTransaction transaction, string projectionName, Position position);
}

public class ProjectionRevisionRepository : IProjectionRevisionRepository
{
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

    public ProjectionRevisionRepository()
    {
    }

    public async Task<Position?> GetStreamPosition(SqlConnection conn, string projectionName)
    {
        var rawPosition = await conn.ExecuteScalarAsync<ulong>(GET_QUERY, new { projectionName });
        if (rawPosition == 0)
            return null;

        return new Position(rawPosition, rawPosition);
    }

    public async Task SetStreamPosition(SqlConnection conn, IDbTransaction transaction, string projectionName, Position position)
    {
        await conn.ExecuteAsync(SET_QUERY, new { projectionName, streamPosition = (long)position.CommitPosition }, transaction);
    }
}
