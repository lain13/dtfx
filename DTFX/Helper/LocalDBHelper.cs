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
        private readonly ITraceLogger _logger;

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

        /// <summary>
        /// 標準ロガーを使用して LocalDB ヘルパーを生成します。
        /// </summary>
        /// <param name="connection">tempdb へ接続済みの SQL Server 接続。</param>
        /// <param name="timeout">SQL コマンドと Bulk Copy のタイムアウト秒数。</param>
        public LocalDBHelper(SqlConnection connection, int timeout)
            : this(connection, timeout, new TraceLogger())
        {
        }

        /// <summary>
        /// 指定した接続、タイムアウト、ロガーで LocalDB ヘルパーを生成します。
        /// </summary>
        /// <param name="connection">tempdb へ接続済みの SQL Server 接続。</param>
        /// <param name="timeout">SQL コマンドと Bulk Copy のタイムアウト秒数。</param>
        /// <param name="logger">DDL などを記録するロガー。</param>
        public LocalDBHelper(SqlConnection connection, int timeout, ITraceLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.Connection = connection;
            this.SqlCommandTimeout = timeout;
            _logger = logger;
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
        /// <param name="reader">列名と型情報を提供するデータリーダー。</param>
        /// <param name="tableName">テーブル名</param>
        public void CreateLocalDBTable(DbDataReader reader, string tableName)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            string tableddl = MSSQLTableCreator.GetCreateTableStatement(reader, physicalTableName);
            _logger.WriteDebug(method, tableddl);

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
        /// <param name="columns">一時テーブルの列名。</param>
        /// <param name="tableName">テーブル名</param>
        public void CreateLocalDBTable(string[] columns, string tableName)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            string physicalTableName = GetLocalDBPhysicalTableName(tableName);
            string tableddl = MSSQLTableCreator.GetCreateTableStatement(columns, physicalTableName);
            _logger.WriteDebug(method, tableddl);

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
        /// <returns>このヘルパーが論理テーブル名を登録済みの場合は <see langword="true"/>。</returns>
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
        /// <returns>列定義だけを持つ空のデータテーブル。</returns>
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
        /// <returns>一時テーブルの全行。テーブルが未作成の場合は <see langword="null"/>。</returns>
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
