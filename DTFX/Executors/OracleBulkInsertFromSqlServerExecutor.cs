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

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// Oracleデータ一括登録処理(OracleBulkInsertFromSqlServer)
    /// MSSQLサーバーからデータを取得してその結果をOracleサーバーに出力する。
    /// </summary>
    public class OracleBulkInsertFromSqlServerExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            if (element.FromDataSource == element.ToDataSource)
            {
                throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleBulkInsertFromSqlServer, "データソースが同の場合は利用できません。" + rawElement.ToString());
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
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleBulkInsertFromSqlServer, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからSqlSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>OracleBulkInsertFromSqlServerElement</returns>
        public OracleBulkInsertFromSqlServerElement CreateElement(XElement rawElement)
        {
            OracleBulkInsertFromSqlServerElement obj = new OracleBulkInsertFromSqlServerElement();
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
        private void WriteToTable(IDataReader reader, OracleBulkInsertFromSqlServerElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            OracleBulkCopy bc = new OracleBulkCopy(
                ServiceContext.GetConnection<OracleConnection>(element.ToDataSource),
                OracleBulkCopyOptions.Default);
            bc.DestinationTableName = element.ToTable;
            bc.BulkCopyTimeout = ServiceContext.SqlCommandTimeout;
            bc.WriteToServer(reader);
        }
    }
}
