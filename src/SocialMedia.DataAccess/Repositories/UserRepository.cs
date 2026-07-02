using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Entities;

namespace SocialMedia.DataAccess.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(User user, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QuerySingleAsync<int>(
            "dbo.usp_User_Create",
            new
            {
                user.Phone,
                user.UserName,
                user.Email,
                user.PasswordHash,
                user.CoverImagePath,
                user.Biography
            },
            transaction,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByPhoneAsync(string phone, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QuerySingleOrDefaultAsync<User>(
            "dbo.usp_User_GetByPhone",
            new { Phone = phone },
            transaction,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QuerySingleOrDefaultAsync<User>(
            "dbo.usp_User_GetById",
            new { UserId = userId },
            transaction,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await QuerySingleOrDefaultAsync<bool>(
            "dbo.usp_User_ExistsByPhone",
            new { Phone = phone },
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

    private async Task<T?> QuerySingleOrDefaultAsync<T>(string storedProcedureName, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var command = CreateCommand(storedProcedureName, parameters, transaction, cancellationToken);

        if (transaction is not null)
        {
            return await transaction.Connection!.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }

        await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
    }

    private async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
