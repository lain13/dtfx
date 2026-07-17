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
    /// Oracleデータ一括登録処理(OracleBulkInsertFromOracle)
    /// Oracleサーバーからデータを取得してその結果をOracleサーバーに出力する。
    /// </summary>
    public class OracleBulkInsertFromOracleExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            if (element.FromDataSource == element.ToDataSource)
            {
                throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleBulkInsertFromOracle, "データソースが同の場合は利用できません。" + rawElement.ToString());
            }
            Logger.WriteDebug(method, element.Value);
            using (var command = new OracleCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<OracleConnection>(element.FromDataSource);
                command.Transaction = ServiceContext.GetTransaction<OracleTransaction>(element.FromDataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    if (!string.IsNullOrEmpty(element.ToTable))
                    {
                        WriteToTable(reader, element);
                    }
                    else
                    {
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleBulkInsertFromOracle, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからSqlSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>OracleBulkInsertFromOracleElement</returns>
        public OracleBulkInsertFromOracleElement CreateElement(XElement rawElement)
        {
            OracleBulkInsertFromOracleElement obj = new OracleBulkInsertFromOracleElement();
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
        /// <param name="element">OracleBulkInsertFromOracleElement</param>
        private void WriteToTable(IDataReader reader, OracleBulkInsertFromOracleElement element)
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
