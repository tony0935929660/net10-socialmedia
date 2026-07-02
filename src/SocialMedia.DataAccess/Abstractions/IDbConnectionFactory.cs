using System.Data;

namespace SocialMedia.DataAccess.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
