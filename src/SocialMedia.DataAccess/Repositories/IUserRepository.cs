using System.Data;
using SocialMedia.Shared.Entities;

namespace SocialMedia.DataAccess.Repositories;

public interface IUserRepository
{
    Task<int> CreateAsync(User user, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<User?> GetByPhoneAsync(string phone, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPhoneAsync(string phone, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
