﻿using System;
using System.Collections.Generic;
using System.Linq;
using Datory;
using SiteServer.CMS.Caches.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Database.Attributes;
using SiteServer.Utils;
using TableColumn = Datory.TableColumn;

namespace SiteServer.CMS.Caches
{
    public static class TableColumnManager
    {
        private static class TableColumnManagerCache
        {
            private static readonly object LockObject = new object();
            private static readonly string CacheKey = DataCacheManager.GetCacheKey(nameof(TableColumnManager));
            //private static readonly FileWatcherClass FileWatcher;

            //static TableColumnManagerCache()
            //{
            //    FileWatcher = new FileWatcherClass(FileWatcherClass.TableColumn);
            //    FileWatcher.OnFileChange += FileWatcher_OnFileChange;
            //}

            //private static void FileWatcher_OnFileChange(object sender, EventArgs e)
            //{
            //    CacheManager.Remove(CacheKey);
            //}

            public static void Clear()
            {
                DataCacheManager.Remove(CacheKey);
                //FileWatcher.UpdateCacheFile();
            }

            private static void Update(Dictionary<string, List<TableColumn>> allDict, List<TableColumn> list,
                string key)
            {
                lock (LockObject)
                {
                    allDict[key] = list;
                }
            }

            private static Dictionary<string, List<TableColumn>> GetAllDictionary()
            {
                var allDict = DataCacheManager.Get<Dictionary<string, List<TableColumn>>>(CacheKey);
                if (allDict != null) return allDict;

                allDict = new Dictionary<string, List<TableColumn>>();
                DataCacheManager.InsertHours(CacheKey, allDict, 24);
                return allDict;
            }

            public static List<TableColumn> GetTableColumnInfoListByCache(string tableName)
            {
                var allDict = GetAllDictionary();

                allDict.TryGetValue(tableName, out var list);

                if (list != null) return list;

                list = DatoryUtils.GetTableColumns(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName);
                Update(allDict, list, tableName);
                return list;
            }
        }

        public static bool CreateTable(string tableName, List<TableColumn> tableColumns, string pluginId, bool isContentTable, out Exception ex)
        {
            ex = null;

            try
            {
                DatoryUtils.CreateTable(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName,
                    tableColumns);
            }
            catch (Exception e)
            {
                ex = e;
                LogUtils.AddErrorLog(pluginId, ex, string.Empty);
                return false;
            }

            if (isContentTable)
            {
                try
                {
                    DatoryUtils.CreateIndex(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName, $"IX_{tableName}_General", $"{ContentAttribute.IsTop} DESC", $"{ContentAttribute.Taxis} DESC", $"{ContentAttribute.Id} DESC");


                    //sqlString =
                    //    $@"CREATE INDEX {DatorySql.GetQuotedIdentifier(DatabaseType, $"IX_{tableName}_General")} ON {DatorySql.GetQuotedIdentifier(DatabaseType, tableName)}({DatorySql.GetQuotedIdentifier(DatabaseType, ContentAttribute.IsTop)} DESC, {DatorySql.GetQuotedIdentifier(DatabaseType, ContentAttribute.Taxis)} DESC, {DatorySql.GetQuotedIdentifier(DatabaseType, ContentAttribute.Id)} DESC)";

                    //ExecuteNonQuery(ConnectionString, sqlString);
                }
                catch (Exception e)
                {
                    ex = e;
                    LogUtils.AddErrorLog(pluginId, ex, string.Empty);
                    return false;
                }

                try
                {
                    DatoryUtils.CreateIndex(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName, $"IX_{tableName}_Taxis", $"{ContentAttribute.Taxis} DESC");

                    //sqlString =
                    //    $@"CREATE INDEX {DatorySql.GetQuotedIdentifier(DatabaseType, $"IX_{tableName}_Taxis")} ON {DatorySql.GetQuotedIdentifier(DatabaseType, tableName)}({DatorySql.GetQuotedIdentifier(DatabaseType, ContentAttribute.Taxis)} DESC)";

                    //ExecuteNonQuery(ConnectionString, sqlString);
                }
                catch (Exception e)
                {
                    ex = e;
                    LogUtils.AddErrorLog(pluginId, ex, string.Empty);
                    return false;
                }
            }

            ClearCache();
            return true;
        }

        public static void AlterTable(string tableName, List<TableColumn> tableColumns, string pluginId, List<string> dropColumnNames = null)
        {
            try
            {
                DatoryUtils.AlterTable(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName,
                    GetRealTableColumns(tableColumns), dropColumnNames);

                ClearCache();
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(pluginId, ex, string.Empty);
            }
        }

        //public static void DropTable(string tableName)
        //{
        //    if (DatoryUtils.DropTable(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName, out var ex))
        //    {
        //        ClearCache();
        //    }
        //    else
        //    {
        //        LogUtils.AddErrorLog(ex);
        //    }
        //}

        private static IList<TableColumn> GetRealTableColumns(IEnumerable<TableColumn> tableColumns)
        {
            var realTableColumns = new List<TableColumn>();
            foreach (var tableColumn in tableColumns)
            {
                if (string.IsNullOrEmpty(tableColumn.AttributeName) || StringUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Id)) || StringUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Guid)) || StringUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.LastModifiedDate)))
                {
                    continue;
                }

                if (tableColumn.DataType == DataType.VarChar && tableColumn.DataLength == 0)
                {
                    tableColumn.DataLength = 2000;
                }
                realTableColumns.Add(tableColumn);
            }

            realTableColumns.InsertRange(0, new List<TableColumn>
            {
                new TableColumn
                {
                    AttributeName = nameof(Entity.Id),
                    DataType = DataType.Integer,
                    IsIdentity = true,
                    IsPrimaryKey = true
                },
                new TableColumn
                {
                    AttributeName = nameof(Entity.Guid),
                    DataType = DataType.VarChar,
                    DataLength = 50
                },
                new TableColumn
                {
                    AttributeName = nameof(Entity.LastModifiedDate),
                    DataType = DataType.DateTime
                }
            });

            return realTableColumns;
        }

        private static List<TableColumn> GetTableColumnInfoList(string tableName)
        {
            return TableColumnManagerCache.GetTableColumnInfoListByCache(tableName);
        }

        public static List<TableColumn> GetTableColumnInfoList(string tableName, List<string> excludeAttributeNameList)
        {
            var list = TableColumnManagerCache.GetTableColumnInfoListByCache(tableName);
            if (excludeAttributeNameList == null || excludeAttributeNameList.Count == 0) return list;

            return list.Where(tableColumnInfo =>
                !StringUtils.ContainsIgnoreCase(excludeAttributeNameList, tableColumnInfo.AttributeName)).ToList();
        }

        private static List<TableColumn> GetTableColumnInfoList(string tableName, DataType excludeDataType)
        {
            var list = TableColumnManagerCache.GetTableColumnInfoListByCache(tableName);

            return list.Where(tableColumnInfo =>
                tableColumnInfo.DataType != excludeDataType).ToList();
        }

        public static TableColumn GetTableColumnInfo(string tableName, string attributeName)
        {
            var list = TableColumnManagerCache.GetTableColumnInfoListByCache(tableName);
            return list.FirstOrDefault(tableColumnInfo =>
                StringUtils.EqualsIgnoreCase(tableColumnInfo.AttributeName, attributeName));
        }

        public static bool IsAttributeNameExists(string tableName, string attributeName)
        {
            var list = TableColumnManagerCache.GetTableColumnInfoListByCache(tableName);
            return list.Any(tableColumnInfo =>
                StringUtils.EqualsIgnoreCase(tableColumnInfo.AttributeName, attributeName));
        }

        public static List<string> GetTableColumnNameList(string tableName)
        {
            var allTableColumnInfoList = GetTableColumnInfoList(tableName);
            return allTableColumnInfoList.Select(tableColumnInfo => tableColumnInfo.AttributeName).ToList();
        }

        public static List<string> GetTableColumnNameList(string tableName, List<string> excludeAttributeNameList)
        {
            var allTableColumnInfoList = GetTableColumnInfoList(tableName, excludeAttributeNameList);
            return allTableColumnInfoList.Select(tableColumnInfo => tableColumnInfo.AttributeName).ToList();
        }

        public static List<string> GetTableColumnNameList(string tableName, DataType excludeDataType)
        {
            var allTableColumnInfoList = GetTableColumnInfoList(tableName, excludeDataType);
            return allTableColumnInfoList.Select(tableColumnInfo => tableColumnInfo.AttributeName).ToList();
        }

        public static void ClearCache()
        {
            TableColumnManagerCache.Clear();
        }
    }

}
