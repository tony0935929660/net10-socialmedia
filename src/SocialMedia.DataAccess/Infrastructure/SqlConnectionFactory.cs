using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.Shared.Constants;

namespace SocialMedia.DataAccess.Infrastructure;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(AppConstants.DefaultConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{AppConstants.DefaultConnectionStringName}' was not found.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
