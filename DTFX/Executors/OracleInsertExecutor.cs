/************************************************************************
* ファイル名:	OracleInsertExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    25年度2期
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
using Oracle.ManagedDataAccess.Client;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// Oracleデータ登録処理(OracleInsert)
    /// ORACLEサーバーに登録SQLを実行する。
    /// </summary>
    public class OracleInsertExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            TraceLog.WriteDebug(method, element.Value);
            using (var command = new OracleCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<OracleConnection>(element.DataSource);
                command.Transaction = ServiceContext.GetTransaction<OracleTransaction>(element.DataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                int result = command.ExecuteNonQuery();
                if (!string.IsNullOrEmpty(element.ToVariable))
                {
                    ServiceContext.SharedVariable.SetValue(element.ToVariable, result);
                    TraceLog.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToVariable, typeof(int));
                }
            }
            if (XSqlElementConstants.AttributeValue.commit.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.CommitTransaction(element.DataSource);
                TraceLog.WriteDebug(method, "コミットされました。データソース名:{0}", element.DataSource);
            }
            else if (XSqlElementConstants.AttributeValue.rollback.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.RollbackTransaction(element.DataSource);
                TraceLog.WriteDebug(method, "ロールバックされました。データソース名:{0}", element.DataSource);
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからOracleInsertElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>OracleInsertElement</returns>
        public OracleInsertElement CreateElement(XElement rawElement)
        {
            OracleInsertElement obj = new OracleInsertElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.DataSource = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.dataSource);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Transaction = GetRawStringValue(rawElement, XSqlElementConstants.AttributeName.transaction);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }
    }
}
