USE [CallCenter]
GO

/****** Object:  StoredProcedure [dbo].[ClientPhoneAddressAndOrderPosMigration]    Script Date: 11/23/2016 1:08:24 PM ******/
DROP PROCEDURE [dbo].[ClientPhoneAddressAndOrderPosMigration]
GO

/****** Object:  StoredProcedure [dbo].[ClientPhoneAddressAndOrderPosMigration]    Script Date: 11/23/2016 1:08:24 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[ClientPhoneAddressAndOrderPosMigration] 
	@FranchiseId AS INT
	, @FranchiseStoreId AS INT
	, @Prefix AS NVARCHAR(5)
	, @PhoneWithoutPrefixLen AS INT
	, @LastStatus AS VARCHAR(30)
	, @UserIdIns AS NVARCHAR(128)
	, @HasMigrateOrder AS BIT
	, @PaymentId AS INT
	, @HasToCommit AS BIT
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

    DECLARE 
		@GuestId BIGINT
		, @ClientId BIGINT 
		, @PhoneId BIGINT
		, @AddressId BIGINT
		, @FirstName NVARCHAR(100)
		, @LastName NVARCHAR(200)
		, @Email NVARCHAR(500)
		, @PhoneNumber VARCHAR(30)
		, @MainAddress NVARCHAR(500)
		, @Reference NVARCHAR(500)
		, @CountPhoneIns INT = 0
		, @CountClientIns INT = 0
		, @CountRelClPhIns INT = 0
		, @CountAddressIns INT = 0
		, @CountRelAdPhIns INT = 0
		, @CountDone INT = 1
		, @CountOrderIns INT = 0
		, @CountOrderAlreadyIns INT = 0
		, @FranchiseCode AS NVARCHAR(10) = NULL

	
	SELECT
		@FranchiseCode = Code
	FROM	
		[CallCenter].[dbo].[Franchise] 
	WHERE
		FranchiseId = @FranchiseId

	IF @FranchiseCode IS NULL
	BEGIN
		PRINT 'No se encontró una franquicia con el identificador: ' + CAST(@FranchiseId AS VARCHAR)
		RETURN -1
	END

	DECLARE MIGRATION_CURSOR CURSOR 
	  LOCAL STATIC READ_ONLY FORWARD_ONLY
	FOR 
	SELECT --TOP 1000
		G.GuestID,
		G.FirstName,
		ISNULL(G.LastName, ''),
		G.EmailAddress,
		CASE WHEN LEN(G.PrimaryPhoneNumber) = @PhoneWithoutPrefixLen AND (G.PrimaryPhoneAreaCode <> @Prefix OR G.PrimaryPhoneAreaCode IS NULL)
			THEN 
				@Prefix + G.PrimaryPhoneNumber
			ELSE 
				G.PrimaryPhoneAreaCode + G.PrimaryPhoneNumber 
		END AS PhoneNumber,
		ISNULL(G.AddressLine1, 'NE'),
		ISNULL(G.AddressLine2, 'NE')
	FROM [AlohaToGo].[dbo].Guest G
	WHERE (LEN(G.PrimaryPhoneAreaCode + G.PrimaryPhoneNumber)  = 10
		OR LEN(G.PrimaryPhoneNumber) = @PhoneWithoutPrefixLen ) AND G.FirstName IS NOT NULL AND G.FirstName <> '.' AND G.FirstName <> 'XXX'

	OPEN MIGRATION_CURSOR
	FETCH NEXT FROM MIGRATION_CURSOR INTO @GuestId, @FirstName, @LastName, @Email, @PhoneNumber, @MainAddress, @Reference

	BEGIN TRANSACTION
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SELECT @ClientId = NULL, @PhoneId = NULL, @AddressId = NULL

		SELECT @PhoneId = ClientPhoneId FROM [CallCenter].[dbo].ClientPhone WHERE Phone = @PhoneNumber

		IF @PhoneId IS NULL
		BEGIN
			INSERT INTO [CallCenter].[dbo].ClientPhone(Phone, UserIdIns, DatetimeIns)
			VALUES(@PhoneNumber, @UserIdIns, GETDATE())
			SET @PhoneId = SCOPE_IDENTITY()
			SET @CountPhoneIns = @CountPhoneIns + 1
		END
	
		SELECT @ClientId = C.ClientId FROM [CallCenter].[dbo].Client C 
			INNER JOIN [CallCenter].[dbo].RelClientPhoneClient Rpc ON Rpc.ClientId = C.ClientId
			INNER JOIN [CallCenter].[dbo].ClientPhone Cp ON Cp.ClientPhoneId = Rpc.ClientPhoneId
		WHERE Phone = @PhoneNumber AND C.FirstName = @FirstName AND C.LastName = @LastName

		IF @ClientId IS NULL
		BEGIN
			INSERT INTO [CallCenter].[dbo].Client(FirstName, LastName, Email, DatetimeIns)
			VALUES(@FirstName, @LastName, @Email, GETDATE())
			SET @ClientId = SCOPE_IDENTITY()
			SET @CountClientIns = @CountClientIns + 1
		END

		IF NOT EXISTS (SELECT * FROM [CallCenter].[dbo].[RelClientPhoneClient] WHERE ClientPhoneId = @PhoneId AND ClientId = @ClientId)
		BEGIN
			INSERT INTO [CallCenter].[dbo].RelClientPhoneClient(ClientId, ClientPhoneId)
			VALUES(@ClientId, @PhoneId)
			SET @CountRelClPhIns = @CountRelClPhIns + 1
		END
	
		SELECT @AddressId = AddressId FROM [CallCenter].[dbo].Address WHERE MainAddress = @MainAddress

		IF @AddressId IS NULL
		BEGIN
			INSERT INTO [CallCenter].[dbo].Address(MainAddress, Reference, CountryName, IsMap)
			VALUES(@MainAddress, @Reference, 'México', 1)
			SET @AddressId = SCOPE_IDENTITY()
			SET @CountAddressIns = @CountAddressIns + 1
		END
	
		IF NOT EXISTS (SELECT * FROM [CallCenter].[dbo].[RelClientPhoneAddress] WHERE ClientPhoneId = @PhoneId AND AddressId = @AddressId)
		BEGIN
			INSERT INTO [CallCenter].[dbo].RelClientPhoneAddress(AddressId, ClientPhoneId)
			VALUES(@AddressId, @PhoneId)
			SET @CountRelAdPhIns = @CountRelAdPhIns + 1
		END

		PRINT 'Contador: ' + CAST(@CountDone AS VARCHAR)

		IF @HasMigrateOrder = 1 
		BEGIN
			EXEC MigrateOrdersByGuestId 
				@LastStatus
				, @PhoneId
				, @GuestId
				, @ClientId
				, @AddressId
				, @FranchiseId
				, @FranchiseStoreId
				, @UserIdIns
				, @PaymentId 
				, @FranchiseCode
				, @CountOrderIns OUTPUT
				, @CountOrderAlreadyIns OUTPUT
		END
		
		
		SET @CountDone = @CountDone + 1
		FETCH NEXT FROM MIGRATION_CURSOR INTO @GuestId, @FirstName, @LastName, @Email, @PhoneNumber, @MainAddress, @Reference
	END

	IF @HasToCommit = 1
	BEGIN
		COMMIT
	END
	ELSE
	BEGIN
		ROLLBACK
	END


	PRINT 'Teléfonos insertados: ' + CAST(@CountPhoneIns AS VARCHAR)
	PRINT 'Clientes insertados: ' + CAST(@CountClientIns AS VARCHAR)
	PRINT 'Rel Tel-Cli insertadas: ' + CAST(@CountRelClPhIns AS VARCHAR)
	PRINT 'Direcciones insertadas: ' + CAST(@CountAddressIns AS VARCHAR)
	PRINT 'Rel Dir-Cli insertadas: ' + CAST(@CountRelAdPhIns AS VARCHAR)
	PRINT 'Órdenes insertadas: ' + CAST(@CountOrderIns AS VARCHAR)
	PRINT 'Órdenes no insertadas por estar repetidas: ' + CAST(@CountOrderAlreadyIns AS VARCHAR)
	PRINT 'Órdenes totales (insertadas y no insertadas): ' + CAST((@CountOrderIns + @CountOrderAlreadyIns) AS VARCHAR)
	SELECT @CountOrderIns = COUNT(*) FROM [AlohaToGo].[dbo].[HistoricalOrderHeader]
	PRINT 'Órdenes totales de la sucursal: '  + CAST(@CountOrderIns AS VARCHAR)
	
	CLOSE MIGRATION_CURSOR
	DEALLOCATE MIGRATION_CURSOR
END

GO


