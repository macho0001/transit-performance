
---run this script in the transit-performance database
--USE transit_performance
--GO

IF OBJECT_ID('getDailyPredictionMetrics') IS NOT NULL
	DROP PROCEDURE dbo.getDailyPredictionMetrics
GO


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.getDailyPredictionMetrics

--Script Version: Master - 1.1.0.0 - generic-all-agencies - 1

--This stored procedure is called by the dailypredictionmetrics API call.  It selects daily prediction metrics for a particular route (or all routes) and time period.

	@route_ids str_val_type READONLY
	,@from_date VARCHAR(255)
	,@to_date VARCHAR(255)

AS

BEGIN
	SET NOCOUNT ON;

	DECLARE @metricstemp AS TABLE
	(
		service_date		VARCHAR(255)
		,route_id			VARCHAR(255)
		,threshold_id		VARCHAR(255)
		,threshold_name		VARCHAR(255)
		,threshold_type		VARCHAR(255)
		,metric_result		FLOAT
	)

	DECLARE @include_route_ids AS TABLE
	(
		route_id	VARCHAR(255)
	)	
	
	IF
		(
		(DATEDIFF(D,@from_date,@to_date) <= 31)
			AND 
				(SELECT COUNT(str_val) FROM @route_ids WHERE str_val NOT IN (SELECT route_id FROM @include_route_ids)) = 0
		)
	
	BEGIN --if a timespan is less than 31 days and routes are only those that should be included, then do the processing, if not return empty set

		INSERT INTO @metricstemp
			SELECT --selects pre-calculated daily metrics from days in the past, if the from_date and to_date are not today 
				service_date
				,route_id
				,threshold_id
				,threshold_name
				,threshold_type
				,metric_result

			FROM dbo.historical_prediction_metrics

			WHERE

				(
						(SELECT COUNT(str_val) FROM @route_ids) = 0
					OR 
						route_id IN (SELECT str_val FROM @route_ids)
				)
				AND 
					service_date >= @from_date
				AND 
					service_date <= @to_date
				AND 
					route_id IN (SELECT route_id FROM @include_route_ids)


	END --if a timespan is less than 31 days and routes are only those that should be included, then do the processing, if not return empty set

	SELECT
		service_date
		,route_id
		,threshold_id
		,threshold_name
		,threshold_type
		,metric_result
	FROM @metricstemp
	ORDER BY
		service_date,route_id,threshold_id


END

GO