using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.ViewModels.Comments;

namespace SocialMedia.DataAccess.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(Comment comment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QuerySingleAsync<int>(
            "dbo.usp_Comment_Create",
            new
            {
                comment.UserId,
                comment.PostId,
                comment.Content
            },
            transaction,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CommentListItemViewModel>> GetByPostIdAsync(int postId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QueryAsync<CommentListItemViewModel>(
            "dbo.usp_Comment_GetByPostId",
            new { PostId = postId },
            transaction,
            cancellationToken).ConfigureAwait(false);
    }

    private static CommandDefinition CreateCommand(string storedProcedureName, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            storedProcedureName,
            parameters,
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
    }

    private async Task<T> QuerySingleAsync<T>(string storedProcedureName, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var command = CreateCommand(storedProcedureName, parameters, transaction, cancellationToken);

        if (transaction is not null)
        {
            return await transaction.Connection!.QuerySingleAsync<T>(command).ConfigureAwait(false);
        }

        await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleAsync<T>(command).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string storedProcedureName, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(storedProcedureName, parameters, transaction, cancellationToken);

        IEnumerable<T> results;
        if (transaction is not null)
        {
            results = await transaction.Connection!.QueryAsync<T>(command).ConfigureAwait(false);
        }
        else
        {
            await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            results = await connection.QueryAsync<T>(command).ConfigureAwait(false);
        }

        return results.ToList();
    }

    private async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
