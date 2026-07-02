using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Constants;

namespace SocialMedia.DataAccess.Infrastructure;

public sealed class SqlTransactionFactory : IDbTransactionFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlTransactionFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection.BeginTransaction();
    }
}
