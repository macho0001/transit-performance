using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;


using IBI.DataAccess.DataSets;

using log4net;

using TransitRealtime;

namespace IBI.DataAccess.Models
{
    public static class Utils
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void BulkInsert(DataTable dataTable, string connectionString)
        {
            if (dataTable.Rows.Count <= 0)
                return;

            using (var connection = new SqlConnection(connectionString))
            {
                BulkInsert(dataTable, connection);
            }
        }

        public static void BulkInsert(DataTable dataTable, SqlConnection connection)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(connection))
            {
                connection.Open();

                BulkInsert(dataTable, sqlBulkCopy);

                connection.Close();
            }
        }

        public static void BulkInsert(DataTable dataTable, SqlBulkCopy sqlBulkCopy)
        {
            BulkInsert(dataTable, sqlBulkCopy, new List<string>());
        }

        public static void BulkInsert(DataTable dataTable, SqlBulkCopy sqlBulkCopy, List<string> excludeColumns)
        {
            if (dataTable.Rows.Count == 0)
            {
                Log.Warn($"Table {dataTable.TableName} is empty...");
                return;
            }

            var insertedDataTable = dataTable.Rows.Cast<DataRow>().Where(x => x.RowState == DataRowState.Added).CopyToDataTable();

            sqlBulkCopy.DestinationTableName = dataTable.TableName;
            sqlBulkCopy.ColumnMappings.Clear();

            foreach (var column in dataTable.Columns.Cast<DataColumn>().Where(column => !excludeColumns.Contains(column.ColumnName)))
            {
                sqlBulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
            }

            sqlBulkCopy.WriteToServer(insertedDataTable);
        }

        public static bool AreEqual(string a, string b)
        {
            return (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) || string.Equals(a, b);
        }

        public static DateTime? GetUtcTimeFromSeconds(ulong seconds)
        {
            return seconds > 0 ? DateTime.SpecifyKind(new DateTime(1970, 1, 1).AddSeconds(seconds), DateTimeKind.Utc) : (DateTime?)null;
        }

        public static ulong GetSecondsFromUtc(DateTime utcDateTime)
        {
            return (ulong)utcDateTime.Subtract(DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc)).Seconds;
        }

        public static List<AlertData> GetAlerts(FeedMessage feedMessage)
        {
            var alerts = new List<AlertData>();

            foreach (var entity in feedMessage.Entities.Where(x => x.Alert != null))
            {
                if (entity.Alert.DescriptionText == null)
                {
                    foreach (var headerTranslation in entity.Alert.HeaderText.Translations)
                    {
                        var alert = GetAlert(feedMessage, entity, null, headerTranslation);
                        alerts.Add(alert);
                    }
                }
                else
                    foreach (var translation in entity.Alert.DescriptionText.Translations)
                    {
                        foreach (var headerTranslation in entity.Alert.HeaderText.Translations)
                        {
                            var alert = GetAlert(feedMessage, entity, translation, headerTranslation);
                            alerts.Add(alert);
                        }
                    }
            }

            return alerts;
        }

        private static AlertData GetAlert(FeedMessage feedMessage,
                                          FeedEntity entity,
                                          TranslatedString.Translation translation,
                                          TranslatedString.Translation headerTranslation)
        {
            return new AlertData
                   {
                       AlertId = entity.Id,
                       Cause = entity.Alert.cause.ToString(),
                       DescriptionLanguage = translation?.Language,
                       DescriptionText = translation?.Text,
                       Effect = entity.Alert.effect.ToString(),
                       GtfsRealtimeVersion = feedMessage.Header?.GtfsRealtimeVersion,
                       HeaderLanguage = headerTranslation.Language,
                       HeaderText = headerTranslation.Text,
                       HeaderTimestamp = feedMessage.Header?.Timestamp ?? 0,
                       Incrementality = feedMessage.Header?.incrementality.ToString(),
                       Url = entity.Alert.Url?.Translations.FirstOrDefault()?.Text,
                       InformedEntities = entity.Alert.InformedEntities.Select(e => new AlertInformedEntityData
                                                                                   {
                                                                                       HeaderTimestamp = feedMessage.Header.Timestamp,
                                                                                       AlertId = entity.Id,
                                                                                       AgencyId = e.AgencyId,
                                                                                       RouteId = e.RouteId,
                                                                                       RouteType = e.RouteType,
                                                                                       StopId = e.StopId,
                                                                                       TripId = e.Trip?.TripId
                                                                                   })
                                                .ToList(),
                       ActivePeriods = entity.Alert.ActivePeriods.Select(a => new AlertActivePeriodData
                                                                              {
                                                                                  HeaderTimestamp = feedMessage.Header.Timestamp,
                                                                                  AlertId = entity.Id,
                                                                                  ActivePeriodEnd = a.End,
                                                                                  ActivePeriodStart = a.Start
                                                                              })
                                             .ToList()
                   };
        }

        public static void InsertAlertsRows(List<AlertData> alerts, ref List<string> previousAlertIds, bool useTemporaryTables)
        {
            var step = "start";

            try
            {
                var alertsUpdateDataSet = new AlertsDataSet();

                if (!alerts.Any())
                {
                    Log.Info("No alerts to save...");
                }
                else
                {
                    Log.Info($"Trying to check {alerts.Count} alert(s).");

                    //var alertsNoFirstTime = _alertsUpdateDataSet.CheckNoFirstTimeList();

                    var alertsSaved = alertsUpdateDataSet.SaveAlerts(alerts, useTemporaryTables);
                    Log.Info($"Saved successfully {alertsSaved} alert(s)");
                }

                step = "checkClosed";

                var currentAlertIds = alerts.Select(y => y.AlertId).ToList();

                var alertIdsToClose = previousAlertIds.Where(x => !currentAlertIds.Contains(x)).ToList();

                var moreToClose = 0;

                if (alertIdsToClose.Any() || !previousAlertIds.Any())
                {
                    var closedSaved = alertsUpdateDataSet.CheckClosedAlerts(alerts,
                                                                            alertIdsToClose.Any() ? alertIdsToClose : new List<string>(),
                                                                            out moreToClose,
                                                                            200,
                                                                            useTemporaryTables);

                    Log.Info(closedSaved > 0 ? $"Closed {closedSaved} alert(s); {moreToClose} more to close..." : "No alerts need to be closed");
                }
                else
                {
                    Log.Info("No alerts need to be closed");
                }

                if (moreToClose == 0)
                    previousAlertIds = alerts.Select(x => x.AlertId).ToList();
                else
                    previousAlertIds.Clear();
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to save data ({step}): {exception.Message}");
                previousAlertIds.Clear();
            }
        }
    }
}
