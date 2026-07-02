using System.Data;
using Microsoft.AspNetCore.Identity;
using Moq;
using SocialMedia.Business.Services;
using SocialMedia.DataAccess.Abstractions;
using SocialMedia.DataAccess.Repositories;
using SocialMedia.Shared.Constants;
using SocialMedia.Shared.Entities;
using SocialMedia.Shared.Exceptions;
using SocialMedia.Shared.ViewModels.Account;
using SocialMedia.Shared.ViewModels.Posts;

namespace SocialMedia.Tests;

public sealed class BusinessServiceTests
{
    [Fact]
    public async Task RegisterAsync_HashesPassword_And_AllowsLoginWithCorrectPassword()
    {
        var userRepository = new Mock<IUserRepository>();
        var capturedUser = default(User);

        userRepository.Setup(r => r.ExistsByPhoneAsync("0912345678", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        userRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
            .Callback<User, IDbTransaction?, CancellationToken>((user, _, _) => capturedUser = user)
            .ReturnsAsync(1);

        var service = new AccountService(userRepository.Object);
        var registeredUser = await service.RegisterAsync(new RegisterViewModel
        {
            Phone = "0912345678",
            UserName = "Alice",
            Email = "alice@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            Biography = "Hello"
        });

        Assert.NotNull(capturedUser);
        Assert.NotEqual("Password123", capturedUser!.PasswordHash);
        Assert.NotEqual(string.Empty, capturedUser.PasswordHash);
        Assert.Equal(1, registeredUser.UserId);

        userRepository.Setup(r => r.GetByPhoneAsync("0912345678", null, It.IsAny<CancellationToken>())).ReturnsAsync(capturedUser);

        var loggedInUser = await service.LoginAsync(new LoginViewModel
        {
            Phone = "0912345678",
            Password = "Password123"
        });

        Assert.Equal(capturedUser.UserId, loggedInUser.UserId);
        Assert.Equal(capturedUser.PasswordHash, loggedInUser.PasswordHash);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User { UserId = 1, Phone = "0912345678", PasswordHash = hasher.HashPassword(new User(), "Password123") };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByPhoneAsync("0912345678", null, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = new AccountService(userRepository.Object);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.LoginAsync(new LoginViewModel
        {
            Phone = "0912345678",
            Password = "WrongPassword"
        }));

        Assert.Equal(ErrorCodes.InvalidCredentials, ex.Code);
    }

    [Fact]
    public async Task RegisterAsync_WhenPhoneExists_ThrowsPhoneAlreadyExists()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.ExistsByPhoneAsync("0912345678", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new AccountService(userRepository.Object);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(new RegisterViewModel
        {
            Phone = "0912345678",
            UserName = "Alice",
            Email = "alice@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            Biography = "Hello"
        }));

        Assert.Equal(ErrorCodes.PhoneAlreadyExists, ex.Code);
        userRepository.Verify(r => r.CreateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("0987654321")]
    public async Task RegisterAsync_ValidPhoneFormat_AllowsRegistration(string phone)
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.ExistsByPhoneAsync(phone, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        userRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var service = new AccountService(userRepository.Object);
        var user = await service.RegisterAsync(new RegisterViewModel
        {
            Phone = phone,
            UserName = "Alice",
            Email = "alice@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            Biography = "Hello"
        });

        Assert.Equal(7, user.UserId);
        Assert.Equal(phone, user.Phone);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("0812345678")]
    [InlineData("09abc45678")]
    public async Task RegisterAsync_InvalidPhoneFormat_ThrowsInvalidPhone(string phone)
    {
        var userRepository = new Mock<IUserRepository>();
        var service = new AccountService(userRepository.Object);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(new RegisterViewModel
        {
            Phone = phone,
            UserName = "Alice",
            Email = "alice@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            Biography = "Hello"
        }));

        Assert.Equal(ErrorCodes.InvalidPhone, ex.Code);
        userRepository.Verify(r => r.ExistsByPhoneAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostService_EditAndDelete_ByNonAuthor_AreRejected()
    {
        var postRepository = new Mock<IPostRepository>();
        postRepository.Setup(r => r.GetByIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostListItemViewModel { PostId = 1, UserId = 2, UserName = "Bob", Content = "Hello", CreatedAt = DateTime.UtcNow });
        var transactionFactory = new Mock<IDbTransactionFactory>();
        var service = new PostService(postRepository.Object, transactionFactory.Object);

        var editEx = await Assert.ThrowsAsync<BusinessException>(() => service.GetForEditAsync(1, 1));
        var deleteEx = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1, 1));

        Assert.Equal(ErrorCodes.UnauthorizedPostOperation, editEx.Code);
        Assert.Equal(ErrorCodes.UnauthorizedPostOperation, deleteEx.Code);
        transactionFactory.Verify(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        postRepository.Verify(r => r.UpdateAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()), Times.Never);
        postRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IDbTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostService_DeleteAsync_CallsCommentDeleteThenPostDelete_And_Commits()
    {
        var postRepository = new Mock<IPostRepository>(MockBehavior.Strict);
        var transactionFactory = new Mock<IDbTransactionFactory>(MockBehavior.Strict);
        var transaction = new Mock<IDbTransaction>(MockBehavior.Strict);

        transaction.SetupGet(t => t.Connection).Returns((IDbConnection?)null);
        transaction.Setup(t => t.Commit());
        transaction.Setup(t => t.Rollback());
        transaction.Setup(t => t.Dispose());

        postRepository.Setup(r => r.GetByIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostListItemViewModel { PostId = 1, UserId = 1, UserName = "Alice", Content = "Hello", CreatedAt = DateTime.UtcNow });
        transactionFactory.Setup(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        var sequence = new MockSequence();
        postRepository.InSequence(sequence)
            .Setup(r => r.DeleteCommentsByPostIdAsync(1, transaction.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        postRepository.InSequence(sequence)
            .Setup(r => r.DeleteAsync(1, 1, transaction.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new PostService(postRepository.Object, transactionFactory.Object);

        await service.DeleteAsync(1, 1);

        transaction.Verify(t => t.Commit(), Times.Once);
        transaction.Verify(t => t.Rollback(), Times.Never);
        transaction.Verify(t => t.Dispose(), Times.Once);
        postRepository.VerifyAll();
        transactionFactory.VerifyAll();
    }

    [Fact]
    public async Task PostService_DeleteAsync_RollsBack_WhenPostDeleteFails()
    {
        var postRepository = new Mock<IPostRepository>(MockBehavior.Strict);
        var transactionFactory = new Mock<IDbTransactionFactory>(MockBehavior.Strict);
        var transaction = new Mock<IDbTransaction>(MockBehavior.Strict);

        transaction.SetupGet(t => t.Connection).Returns((IDbConnection?)null);
        transaction.Setup(t => t.Commit());
        transaction.Setup(t => t.Rollback());
        transaction.Setup(t => t.Dispose());

        postRepository.Setup(r => r.GetByIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostListItemViewModel { PostId = 1, UserId = 1, UserName = "Alice", Content = "Hello", CreatedAt = DateTime.UtcNow });
        transactionFactory.Setup(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        var sequence = new MockSequence();
        postRepository.InSequence(sequence)
            .Setup(r => r.DeleteCommentsByPostIdAsync(1, transaction.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        postRepository.InSequence(sequence)
            .Setup(r => r.DeleteAsync(1, 1, transaction.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        var service = new PostService(postRepository.Object, transactionFactory.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1, 1));

        transaction.Verify(t => t.Commit(), Times.Never);
        transaction.Verify(t => t.Rollback(), Times.Once);
        transaction.Verify(t => t.Dispose(), Times.Once);
    }
}
