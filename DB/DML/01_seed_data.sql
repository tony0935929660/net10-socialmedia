USE [SocialMediaDb];
GO

DECLARE @UserPhoneA VARCHAR(10) = '0912345678';
DECLARE @UserPhoneB VARCHAR(10) = '0998765432';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Phone = @UserPhoneA)
BEGIN
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
		@UserPhoneA,
		N'Alice',
		N'alice@example.com',
		N'AQAAAAIAAYagAAAAEMockHashForAliceUserDoNotUseInProduction==',
		NULL,
		N'Hi, I am Alice and I enjoy sharing travel photos.'
	);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Phone = @UserPhoneB)
BEGIN
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
		@UserPhoneB,
		N'Bob',
		N'bob@example.com',
		N'AQAAAAIAAYagAAAAEMockHashForBobUserDoNotUseInProduction==',
		NULL,
		N'Bob writes about coding and coffee.'
	);
END;

DECLARE @AliceUserId INT = (SELECT UserId FROM dbo.Users WHERE Phone = @UserPhoneA);
DECLARE @BobUserId INT = (SELECT UserId FROM dbo.Users WHERE Phone = @UserPhoneB);

IF NOT EXISTS (SELECT 1 FROM dbo.Posts WHERE UserId = @AliceUserId AND Content = N'Just arrived in Tainan, the weather is perfect!')
BEGIN
	INSERT INTO dbo.Posts
	(
		UserId,
		Content,
		ImagePath,
		CreatedAt
	)
	VALUES
	(
		@AliceUserId,
		N'Just arrived in Tainan, the weather is perfect!',
		NULL,
		DATEADD(MINUTE, -90, SYSUTCDATETIME())
	);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Posts WHERE UserId = @BobUserId AND Content = N'Building a .NET 10 app today, wish me luck!')
BEGIN
	INSERT INTO dbo.Posts
	(
		UserId,
		Content,
		ImagePath,
		CreatedAt
	)
	VALUES
	(
		@BobUserId,
		N'Building a .NET 10 app today, wish me luck!',
		NULL,
		DATEADD(MINUTE, -60, SYSUTCDATETIME())
	);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Posts WHERE UserId = @AliceUserId AND Content = N'Tried a new coffee place near the station.')
BEGIN
	INSERT INTO dbo.Posts
	(
		UserId,
		Content,
		ImagePath,
		CreatedAt
	)
	VALUES
	(
		@AliceUserId,
		N'Tried a new coffee place near the station.',
		NULL,
		DATEADD(MINUTE, -30, SYSUTCDATETIME())
	);
END;

DECLARE @PostAliceTripId INT = (SELECT TOP (1) PostId FROM dbo.Posts WHERE UserId = @AliceUserId AND Content = N'Just arrived in Tainan, the weather is perfect!');
DECLARE @PostBobDotNetId INT = (SELECT TOP (1) PostId FROM dbo.Posts WHERE UserId = @BobUserId AND Content = N'Building a .NET 10 app today, wish me luck!');
DECLARE @PostAliceCoffeeId INT = (SELECT TOP (1) PostId FROM dbo.Posts WHERE UserId = @AliceUserId AND Content = N'Tried a new coffee place near the station.');

IF @PostAliceTripId IS NOT NULL
	AND NOT EXISTS (SELECT 1 FROM dbo.Comments WHERE PostId = @PostAliceTripId AND UserId = @BobUserId AND Content = N'Enjoy your trip! Any food recommendations?')
BEGIN
	INSERT INTO dbo.Comments
	(
		UserId,
		PostId,
		Content,
		CreatedAt
	)
	VALUES
	(
		@BobUserId,
		@PostAliceTripId,
		N'Enjoy your trip! Any food recommendations?',
		DATEADD(MINUTE, -50, SYSUTCDATETIME())
	);
END;

IF @PostBobDotNetId IS NOT NULL
	AND NOT EXISTS (SELECT 1 FROM dbo.Comments WHERE PostId = @PostBobDotNetId AND UserId = @AliceUserId AND Content = N'Good luck! Remember to write tests first.')
BEGIN
	INSERT INTO dbo.Comments
	(
		UserId,
		PostId,
		Content,
		CreatedAt
	)
	VALUES
	(
		@AliceUserId,
		@PostBobDotNetId,
		N'Good luck! Remember to write tests first.',
		DATEADD(MINUTE, -40, SYSUTCDATETIME())
	);
END;

IF @PostAliceCoffeeId IS NOT NULL
	AND NOT EXISTS (SELECT 1 FROM dbo.Comments WHERE PostId = @PostAliceCoffeeId AND UserId = @BobUserId AND Content = N'Sounds great, I need that place name!')
BEGIN
	INSERT INTO dbo.Comments
	(
		UserId,
		PostId,
		Content,
		CreatedAt
	)
	VALUES
	(
		@BobUserId,
		@PostAliceCoffeeId,
		N'Sounds great, I need that place name!',
		DATEADD(MINUTE, -10, SYSUTCDATETIME())
	);
END;
GO
