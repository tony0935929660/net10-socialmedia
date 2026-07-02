using System.Data;

namespace SocialMedia.DataAccess.Abstractions;

public interface IDbTransactionFactory
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
