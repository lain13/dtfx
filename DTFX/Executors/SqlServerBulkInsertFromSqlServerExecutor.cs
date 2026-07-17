/************************************************************************
* ファイル名:	SqlServerBulkInsertFromSqlServerExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   24.1-001-01     2024/03/27  姜　恵遠    PostgreSQL Bulk Insert対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using IF.Batch.DTFX.Service;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Elements;
using IF.Batch.Common.Diagnostics;
using IF.Batch.DTFX.Helper;
using IF.Batch.Common.Helper;
using System.Data.Common;
using IF.Batch.DTFX.Exceptions;
using System.Data;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// MSSQLデータ一括登録処理(SqlServerBulkInsertFromSqlServer)
    /// MSSQLサーバーからデータを取得してその結果をMSSQLサーバーに出力する。
    /// </summary>
    public class SqlServerBulkInsertFromSqlServerExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            if (element.FromDataSource == element.ToDataSource)
            {
                throw new AppConfigurationException(XSqlElementConstants.ElementName.SqlServerBulkInsertFromSqlServer, "データソースが同の場合は利用できません。" + rawElement.ToString());
            }
            using (var command = new SqlCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<SqlConnection>(element.FromDataSource);
                command.Transaction = ServiceContext.GetTransaction<SqlTransaction>(element.FromDataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!string.IsNullOrEmpty(element.ToTable))
                    {
                        WriteToTable(reader, element);
                    }
                    else
                    {
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.SqlServerBulkInsertFromSqlServer, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからSqlSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>SqlServerBulkInsertFromSqlServerElement</returns>
        public SqlServerBulkInsertFromSqlServerElement CreateElement(XElement rawElement)
        {
            SqlServerBulkInsertFromSqlServerElement obj = new SqlServerBulkInsertFromSqlServerElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.FromDataSource = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.fromDataSource);
            obj.ToDataSource = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toDataSource);
            obj.ToTable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toTable);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }

        /// <summary>
        /// DBから取得した結果をコピー先データソースのテーブルに出力します。
        /// </summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="element">SqlServerBulkInsertFromSqlServerElement</param>
        private void WriteToTable(IDataReader reader, SqlServerBulkInsertFromSqlServerElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            SqlBulkCopy bc = new SqlBulkCopy(
                ServiceContext.GetConnection<SqlConnection>(element.ToDataSource),
                SqlBulkCopyOptions.KeepNulls,
                ServiceContext.GetTransaction<SqlTransaction>(element.ToDataSource));
            bc.DestinationTableName = element.ToTable;
            bc.BulkCopyTimeout = ServiceContext.SqlCommandTimeout;
            bc.WriteToServer(reader);
        }
    }
}
