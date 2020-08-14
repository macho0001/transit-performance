/*
This file can execute teh setup all of all the script need to create the project from scratch instead of running individually.	
Make sure to uncomment the database you want to use in the create functions and types.
--------------------------------------------------------------------------------------
*/

:r C:\yourPath\CreateDatabase.sql
--:r C:\yourPath\CreateSQLlogin.sql


:r C:\yourPath\CreateFunctionsAndTypes.sql
:r C:\yourPath\CreateInitializationTables.sql
:r C:\yourPath\PreProcessDaily.sql
:r C:\yourPath\PostProcessDaily.sql
:r C:\yourPath\ProcessPredictionAccuracyDaily.sql
:r C:\yourPath\CreateTodayRTProcess.sql
:r C:\yourPath\PreProcessToday.sql
:r C:\yourPath\ProcessRTEvent.sql
:r C:\yourPath\ProcessCurrentMetrics.sql
:r C:\yourPath\UpdateGTFSNext.sql
:r C:\yourPath\ClearData.sql
:r C:\yourPath\getCurrentMetrics.sql
:r C:\yourPath\getDailyMetrics.sql
:r C:\yourPath\getDwellTimes.sql
:r C:\yourPath\getHeadwayTimes.sql
:r C:\yourPath\getTravelTimes.sql
:r C:\yourPath\getDailyPredictionMetrics.sql
:r C:\yourPath\getPredictionMetrics.sql
:r C:\yourPath\getEvents.sql
:r C:\yourPath\getPastAlerts.sql
:r C:\yourPath\getPastAlertsVersions.sql
:r C:\yourPath\getPastAlertsActivePeriods.sql
:r C:\yourPath\getPastAlertsInformedEntities.sql