/************************************************************************
* ファイル名:	PostgreSqlBulkInsertFromSqlServerExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2024/03/27	姜　恵遠	新規作成
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
using Oracle.ManagedDataAccess.Client;
using Npgsql;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// PostgreSqlデータ一括登録処理(PostgreSqlBulkInsertFromSqlServer)
    /// SqlServerサーバーからデータを取得してその結果をPostgreSqlサーバーに出力する。
    /// </summary>
    public class PostgreSqlBulkInsertFromSqlServerExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            if (element.FromDataSource == element.ToDataSource)
            {
                throw new AppConfigurationException(XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromSqlServer, "データソースが同の場合は利用できません。" + rawElement.ToString());
            }
            Logger.WriteDebug(method, element.Value);
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
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromSqlServer, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからPostgreSqlBulkInsertFromSqlServerElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>PostgreSqlBulkInsertFromSqlServerElement</returns>
        public PostgreSqlBulkInsertFromSqlServerElement CreateElement(XElement rawElement)
        {
            PostgreSqlBulkInsertFromSqlServerElement obj = new PostgreSqlBulkInsertFromSqlServerElement();
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
        /// <param name="element">PostgreSqlBulkInsertFromSqlServerElement</param>
        private void WriteToTable(IDataReader reader, PostgreSqlBulkInsertFromSqlServerElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            NpgsqlBulkCopy bc = new NpgsqlBulkCopy(
                ServiceContext.GetConnection<NpgsqlConnection>(element.ToDataSource));
            bc.DestinationTableName = element.ToTable;
            bc.BulkCopyTimeout = ServiceContext.SqlCommandTimeout;
            bc.WriteToServer(reader);
        }
    }
}
