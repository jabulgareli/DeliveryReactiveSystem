USE [CallCenter]
GO

/****** Object:  StoredProcedure [dbo].[MigrateOrdersByGuestId]    Script Date: 11/23/2016 1:08:00 PM ******/
DROP PROCEDURE [dbo].[MigrateOrdersByGuestId]
GO

/****** Object:  StoredProcedure [dbo].[MigrateOrdersByGuestId]    Script Date: 11/23/2016 1:08:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[MigrateOrdersByGuestId]
	@LastStatus  AS VARCHAR(30)
	, @PhoneId AS BIGINT
	, @GuestId AS BIGINT
	, @ClientId AS BIGINT
	, @AddressId AS BIGINT
	, @FranchiseId AS INT
	, @FranchiseStoreId AS INT
	, @UserIdIns AS NVARCHAR(128)
	, @PaymentId AS INT
	, @FranchiseCode AS NVARCHAR(10)
	, @CountOrderIns INT OUTPUT
	, @CountOrderAlreadyIns INT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE		 
		@OrderId BIGINT
		, @OrderAtoId NVARCHAR(50)
		, @CheckId INT
		, @LastModified DATETIME
		, @OrderTime DATETIME
		, @CloseTime DATETIME
		, @PromiseTime DATETIME
		, @SubTotal DECIMAL(18, 4)
		, @Tax DECIMAL(18, 4)
		, @PaymentTotal DECIMAL(18, 4)
		, @PosOrderId BIGINT


	DECLARE MIGRATION_ORDERS_CURSOR CURSOR 
	  LOCAL STATIC READ_ONLY FORWARD_ONLY
	FOR 
	SELECT 
		Ho.OrderID AS OrderId
		, CAST(Ho.OrderID AS NVARCHAR(50)) AS OrderAtoId
		, Ho.TableID
		, Ho.LastModifiedTimestamp
		, Ho.OrderTime
		, Ho.ClosedTime
		, Ho.DispatchTime
		, CAST(Ho.SubTotal AS DECIMAL(18, 4))
		, CAST(Ho.Tax AS DECIMAL(18, 4))
		, CAST(Ho.PaymentTotal AS DECIMAL(18, 4))

	FROM [AlohaToGo].[dbo].[HistoricalOrderHeader] Ho
	WHERE GuestID = @GuestId 

		OPEN MIGRATION_ORDERS_CURSOR
	FETCH NEXT FROM MIGRATION_ORDERS_CURSOR INTO @OrderId, @OrderAtoId, @CheckId, @LastModified, @OrderTime, @CloseTime, @PromiseTime, @SubTotal, @Tax, @PaymentTotal

	WHILE @@FETCH_STATUS = 0
	BEGIN

		IF EXISTS( SELECT * FROM [CallCenter].[dbo].[OrderToStore] Ot WHERE @OrderAtoId = Ot.OrderAtoId)
		BEGIN
			SET @CountOrderAlreadyIns = @CountOrderAlreadyIns + 1
		END
		ELSE
		BEGIN

			INSERT INTO [CallCenter].[dbo].[PosOrder](FranchiseCode, CheckId, GuidId, OrderDatetime, UserId, Subtotal, Taxes, Total)
			VALUES(@FranchiseCode, @CheckId, NEWID(), @OrderTime, @UserIdIns, @SubTotal, @Tax, @PaymentTotal)
			SET @PosOrderId = SCOPE_IDENTITY()

			INSERT INTO [CallCenter].[dbo].[PosOrderItem](PosOrderId, CheckItemId, ItemId, Name, Price, LevelItem, ParentId)
			SELECT
				@PosOrderId
				, PosEntryID
				, MenuItemID
				, Name
				, UnitPrice
				, ItemLevel
				, NULL
			FROM[AlohaToGo].[dbo].[HistoricalOrderItem]
			WHERE OrderID = @OrderId
			ORDER BY ItemID

			SET @CountOrderIns = @CountOrderIns + 1

			INSERT INTO [CallCenter].[dbo].[OrderToStore](LastStatus, LastUpdate, ClientPhoneId, AddressId, ClientId, FranchiseId, 
				FranchiseStoreId, PosOrderId, UserInsId, StartDatetime, EndDatetime, OrderAtoId, OrderMode, OrderModeCharge, PromiseTime, PaymentId, IsCanceled,
				DateTimeCanceled, FailedStatusCounter, PosOrderStatus, ExtraNotes, InputType)
			VALUES(@LastStatus, @LastModified, @PhoneId, @AddressId, @ClientId, @FranchiseId, 
				@FranchiseStoreId, @PosOrderId, @UserIdIns, @OrderTime, @LastModified, @OrderAtoId, 'Delivery', 0, @LastModified, @PaymentId, NULL,
				NULL, 0, 500, 'Migrado', 2000)

		END

		FETCH NEXT FROM MIGRATION_ORDERS_CURSOR INTO @OrderId, @OrderAtoId, @CheckId, @LastModified, @OrderTime, @CloseTime, @PromiseTime, @SubTotal, @Tax, @PaymentTotal
	END

	CLOSE MIGRATION_ORDERS_CURSOR
	DEALLOCATE MIGRATION_ORDERS_CURSOR
END

GO


