SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'TF' and name = 'GetCommodityChannelIndexValues')
BEGIN
	PRINT 'Dropping "GetCommodityChannelIndexValues" function...'
	DROP FUNCTION GetCommodityChannelIndexValues
END
GO

PRINT 'Creating "GetCommodityChannelIndexValues" function...'
GO

-- =============================================
-- Author:		Steve Whitmire Jr.
-- Create date: 08-20-2019
-- Description:	Returns the Commodity Channel Index (CCI) calculations for a given company.
-- =============================================
CREATE FUNCTION [dbo].[GetCommodityChannelIndexValues] 
(
	@companyId int,
	@startDate date,
	@endDate date,
	@cciPeriod int
)
RETURNS @cciValues TABLE
(
	[QuoteId] INT UNIQUE, 
	[CompanyId] INT,  
	[Date] DATE UNIQUE, 
	[CCI] DECIMAL(9, 3)
)
AS
BEGIN

	-- Verified 09/02/2019 --

	/*********************************************************************************************
		Table to hold CCI typical price calculations for the @cciPeriod.
	*********************************************************************************************/
	DECLARE @typicalPriceCalcs TABLE
	(
		[QuoteId] INT UNIQUE,
		[CompanyId] INT,
		[Date] DATE UNIQUE,
		[TypicalPrice] DECIMAL(9, 3)
	);

	INSERT INTO @typicalPriceCalcs
	SELECT [Id] as [QuoteId],
		   [CompanyId],
		   [Date],
		   ([High] + [Low] + [Close]) / 3.0 as [TypicalPrice]
	FROM GetCompanyQuotes(@companyId)
	ORDER BY [Date]

	/*********************************************************************************************
		Table to hold CCI moving average calculations for the @cciPeriod.
	*********************************************************************************************/
	DECLARE @movingAvgCalcs TABLE
	(
		[QuoteId] INT UNIQUE,
		[Date] DATE UNIQUE,
		[TypicalPrice] DECIMAL(9, 3),
		[MovingAverage] DECIMAL(9, 3)
	);

	INSERT INTO @movingAvgCalcs
	SELECT [QuoteId],
		   [Date],
		   [TypicalPrice],
		   (SELECT AVG(TypicalPrice) FROM (SELECT [TypicalPrice] FROM @typicalPriceCalcs typicalPriceInner WHERE typicalPriceInner.QuoteId <= typicalPriceOuter.QuoteId AND typicalPriceInner.QuoteId >= (typicalPriceOuter.QuoteId - @cciPeriod + 1)) as MovingAverageInner) as [MovingAverage]
	FROM @typicalPriceCalcs typicalPriceOuter
	ORDER BY [Date]

	/*********************************************************************************************
		Table to hold CCI mean deviation calculations for the @cciPeriod.
	*********************************************************************************************/
	DECLARE @cciMeanDeviationCalcs TABLE
	(
		[QuoteId] INT UNIQUE,
		[Date] DATE UNIQUE,
		[MeanDeviation] DECIMAL(9, 5)
	);

	INSERT INTO @cciMeanDeviationCalcs
	SELECT [QuoteId],
		   [Date],
		   (SELECT AVG(ABS(TypicalPrice - MovingAverageOuter)) 
		    FROM (SELECT [TypicalPrice], 
						 movingAverageOuter.MovingAverage as [MovingAverageOuter]
			      FROM @movingAvgCalcs movingAverageInner 
				  WHERE movingAverageInner.QuoteId <= movingAverageOuter.QuoteId AND movingAverageInner.QuoteId >= (movingAverageOuter.QuoteId - @cciPeriod + 1)) as MovingAverageInner) as [MeanDeviation]
	FROM @movingAvgCalcs movingAverageOuter
	ORDER BY [Date]

	/*********************************************************************************************
		Table to hold the CCI values over the @cciPeriod for each quote. 
	*********************************************************************************************/
	INSERT INTO @cciValues
	SELECT typicalPriceCalcs.QuoteId as [QuoteId], 
	       typicalPriceCalcs.CompanyId as [CompanyId], 
	       typicalPriceCalcs.Date as [Date],
		   CASE WHEN meanDeviationCalcs.MeanDeviation = 0
			    THEN 10111.00
				ELSE (typicalPriceCalcs.TypicalPrice - movingAvgCalcs.MovingAverage) / (.015 * meanDeviationCalcs.MeanDeviation)
				END as CCI
	FROM @typicalPriceCalcs typicalPriceCalcs
		INNER JOIN @movingAvgCalcs movingAvgCalcs ON typicalPriceCalcs.QuoteId = movingAvgCalcs.QuoteId
		INNER JOIN @cciMeanDeviationCalcs meanDeviationCalcs on typicalPriceCalcs.QuoteId = meanDeviationCalcs.QuoteId
	WHERE typicalPriceCalcs.CompanyId = @companyId AND typicalPriceCalcs.[Date] >= @startDate AND typicalPriceCalcs.[Date] <= @endDate
	ORDER BY typicalPriceCalcs.[Date]

	RETURN;
END
GO