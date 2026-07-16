/************************************************************************
* ファイル名:	LocalDBHelper.cs
* 概要: SQL Server一時テーブルの生成・管理・データ読み書きを行うヘルパークラス
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*	25.1-001-02		2013/09/24	姜　恵遠	SqlCommandTimeout追加
*	25.1-001-03		2013/10/10	姜　恵遠	一時テーブル名の頭に#を追加
*                                           一時テーブル物理削除処理機能削除
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using IF.Batch.Common.Diagnostics;
using System.Reflection;

namespace IF.Batch.DTFX.Helper
{
    /// <summary>
    /// SQL Serverの一時テーブルを管理するヘルパークラス。
    /// テーブルのDDL生成、バルクコピー、データ取得機能を提供します。
    /// </summary>
    public class LocalDBHelper
    {
        /// <summary>
        /// 一時DBのデータソース
        /// </summary>
        public SqlConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// 一時DBのSqlCommandTimeout
        /// </summary>
        public int SqlCommandTimeout
        {
            get;
            private set;
        }

        /// <summary>
        /// 一時テーブル生成時利用する頭文字
        /// </summary>
        private const string __TEMP__ = "#__TEMP__";

        /// <summary>
        /// 一時テーブル生成時利用する頭文字
        /// </summary>
        private readonly string _tableNamePrefix = __TEMP__ + Guid.NewGuid().ToString().Substring(0, 8) + "_";

        /// <summary>
        /// 生成された一時テーブルのリスト
        /// </summary>
        private readonly SynchronizedCollection<string> _logicalTableNames = new SynchronizedCollection<string>();

        public LocalDBHelper(SqlConnection connection, int timeout)
        {
            this.Connection = connection;
            this.SqlCommandTimeout = timeout;
        }

        /// <summary>
        /// 一時DBに生成されたテーブルの物理テーブル名を取得する。
        /// </summary>
        /// <param name="tableName">論理テーブル名</param>
        /// <returns>物理テーブル名</returns>
        public string GetLocalDBPhysicalTableName(string tableName)
        {
            return _tableNamePrefix + tableName;
        }

        /// <summary>
        /// 一時DBテーブルを生成する。
        /// </summary>
        /// <param name="reader">データ元</param>
        /// <param name="tableName">テーブル名</param>
        public void CreateLocalDBTable(DbDataReader reader, string tableName)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            string tableddl = MSSQLTableCreator.GetCreateTableStatement(reader, physicalTableName);
            TraceLog.WriteDebug(method, tableddl);

            using (var command = new SqlCommand(tableddl))
            {
                command.Connection = Connection;
                command.CommandTimeout = SqlCommandTimeout;
                command.ExecuteNonQuery();
            }
            _logicalTableNames.Add(tableName);
        }

        /// <summary>
        /// 一時DBテーブルを生成する。
        /// </summary>
        /// <param name="reader">データ元</param>
        /// <param name="tableName">テーブル名</param>
        public void CreateLocalDBTable(string[] columns, string tableName)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            string tableddl = MSSQLTableCreator.GetCreateTableStatement(columns, physicalTableName);
            TraceLog.WriteDebug(method, tableddl);

            using (var command = new SqlCommand(tableddl))
            {
                command.Connection = Connection;
                command.CommandTimeout = SqlCommandTimeout;
                command.ExecuteNonQuery();
            }
            _logicalTableNames.Add(tableName);
        }

        /// <summary>
        /// 一時DBにテーブルが生成されたか確認します。
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <returns></returns>
        public bool IsLocalDBTableCreated(string tableName)
        {
            return _logicalTableNames.Contains(tableName);
        }

        /// <summary>
        /// 一時DBにデータを書き込む。
        /// </summary>
        /// <param name="reader">データ元</param>
        /// <param name="tableName">テーブル名</param>
        public void WriteToServer(DbDataReader reader, string tableName)
        {
            if (!IsLocalDBTableCreated(tableName))
            {
                CreateLocalDBTable(reader, tableName);
            }
            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            SqlBulkCopy bc = new SqlBulkCopy(Connection, SqlBulkCopyOptions.KeepNulls, null);
            bc.DestinationTableName = physicalTableName;
            bc.BulkCopyTimeout = SqlCommandTimeout;
            bc.WriteToServer(reader);
        }

        /// <summary>
        /// 一時DBにデータを書き込む。
        /// </summary>
        /// <param name="list">データ元</param>
        /// <param name="tableName">テーブル名</param>
        /// <param name="hasHeader">ヘッダを含む</param>
        public void WriteToServer(List<string[]> list, string tableName, bool hasHeader)
        {
            if (!IsLocalDBTableCreated(tableName))
            {
                if (hasHeader)
                {
                    CreateLocalDBTable(list.First(), tableName);
                }
                else
                {
                    CreateLocalDBTable(MSSQLTableCreator.CreateColumnNames("COLUMN", list.First().Length), tableName);
                }
            }
            if (hasHeader && list.Count < 2)
            {
                return;
            }
            DataTable table = GetEmptyDataTable(tableName);
            for (int i = hasHeader ? 1 : 0; i < list.Count; i++)
            {
                table.Rows.Add(list[i]);
            }
            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            SqlBulkCopy bc = new SqlBulkCopy(Connection, SqlBulkCopyOptions.KeepNulls, null);
            bc.DestinationTableName = physicalTableName;
            bc.BulkCopyTimeout = SqlCommandTimeout;
            bc.WriteToServer(table);
        }

        /// <summary>
        /// ローカルテーブルのSchemaを取得する
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <returns></returns>
        public DataTable GetEmptyDataTable(string tableName)
        {
            string sql = "SELECT TOP 0 * FROM " + GetLocalDBPhysicalTableName(tableName);
            using (SqlCommand command = new SqlCommand(sql, Connection))
            {
                command.CommandTimeout = SqlCommandTimeout;
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        /// <summary>
        /// 一時DBテーブルの全データを取得する。
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <returns></returns>
        public DataTable SelectAllTable(string tableName)
        {
            if (!IsLocalDBTableCreated(tableName))
            {
                return null;
            }

            string sql = "SELECT * FROM " + GetLocalDBPhysicalTableName(tableName); ;
            using (SqlCommand command = new SqlCommand(sql, Connection))
            {
                command.CommandTimeout = SqlCommandTimeout;
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }
    }
}
