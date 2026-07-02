USE [SocialMediaDb];
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Create
	@Phone VARCHAR(10),
	@UserName NVARCHAR(50),
	@Email NVARCHAR(256),
	@PasswordHash NVARCHAR(MAX),
	@CoverImagePath NVARCHAR(500) = NULL,
	@Biography NVARCHAR(500) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Users
	(
		Phone,
		UserName,
		Email,
		PasswordHash,
		CoverImagePath,
		Biography
	)
	VALUES
	(
		@Phone,
		@UserName,
		@Email,
		@PasswordHash,
		@CoverImagePath,
		@Biography
	);

	SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetByPhone
	@Phone VARCHAR(10)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		UserId,
		Phone,
		UserName,
		Email,
		PasswordHash,
		CoverImagePath,
		Biography,
		CreatedAt
	FROM dbo.Users
	WHERE Phone = @Phone;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
	@UserId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		UserId,
		Phone,
		UserName,
		Email,
		PasswordHash,
		CoverImagePath,
		Biography,
		CreatedAt
	FROM dbo.Users
	WHERE UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_ExistsByPhone
	@Phone VARCHAR(10)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT CAST(CASE WHEN EXISTS
	(
		SELECT 1
		FROM dbo.Users
		WHERE Phone = @Phone
	) THEN 1 ELSE 0 END AS BIT) AS IsExists;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_Create
	@UserId INT,
	@Content NVARCHAR(2000),
	@ImagePath NVARCHAR(500) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Posts
	(
		UserId,
		Content,
		ImagePath
	)
	VALUES
	(
		@UserId,
		@Content,
		@ImagePath
	);

	SELECT CAST(SCOPE_IDENTITY() AS INT) AS PostId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_GetAll
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		p.PostId,
		p.UserId,
		u.UserName,
		p.Content,
		p.ImagePath,
		p.CreatedAt,
		COUNT(c.CommentId) AS CommentCount
	FROM dbo.Posts AS p
	INNER JOIN dbo.Users AS u ON u.UserId = p.UserId
	LEFT JOIN dbo.Comments AS c ON c.PostId = p.PostId
	GROUP BY
		p.PostId,
		p.UserId,
		u.UserName,
		p.Content,
		p.ImagePath,
		p.CreatedAt
	ORDER BY p.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_GetById
	@PostId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		p.PostId,
		p.UserId,
		u.UserName,
		p.Content,
		p.ImagePath,
		p.CreatedAt
	FROM dbo.Posts AS p
	INNER JOIN dbo.Users AS u ON u.UserId = p.UserId
	WHERE p.PostId = @PostId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_GetByUserId
	@UserId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		p.PostId,
		p.UserId,
		p.Content,
		p.ImagePath,
		p.CreatedAt,
		COUNT(c.CommentId) AS CommentCount
	FROM dbo.Posts AS p
	LEFT JOIN dbo.Comments AS c ON c.PostId = p.PostId
	WHERE p.UserId = @UserId
	GROUP BY
		p.PostId,
		p.UserId,
		p.Content,
		p.ImagePath,
		p.CreatedAt
	ORDER BY p.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_Update
	@PostId INT,
	@UserId INT,
	@Content NVARCHAR(2000),
	@ImagePath NVARCHAR(500) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Posts
	SET
		Content = @Content,
		ImagePath = @ImagePath
	WHERE PostId = @PostId
	  AND UserId = @UserId;

	SELECT @@ROWCOUNT AS AffectedRows;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Post_Delete
	@PostId INT,
	@UserId INT
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Posts
	WHERE PostId = @PostId
	  AND UserId = @UserId;

	SELECT @@ROWCOUNT AS AffectedRows;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Comment_Create
	@UserId INT,
	@PostId INT,
	@Content NVARCHAR(1000)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Comments
	(
		UserId,
		PostId,
		Content
	)
	VALUES
	(
		@UserId,
		@PostId,
		@Content
	);

	SELECT CAST(SCOPE_IDENTITY() AS INT) AS CommentId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Comment_GetByPostId
	@PostId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		c.CommentId,
		c.UserId,
		u.UserName,
		c.PostId,
		c.Content,
		c.CreatedAt
	FROM dbo.Comments AS c
	INNER JOIN dbo.Users AS u ON u.UserId = c.UserId
	WHERE c.PostId = @PostId
	ORDER BY c.CreatedAt ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Comment_DeleteByPostId
	@PostId INT
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Comments
	WHERE PostId = @PostId;

	SELECT @@ROWCOUNT AS AffectedRows;
END;
GO
