/************************************************************************
* ファイル名:	PostgreSqlUpdateExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	23.1-001-01		2023/02/15	姜　恵遠	新規作成
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
using Npgsql;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// PostgreSQLデータ更新処理(PostgreSQLUpdate)
    /// PostgreSQLサーバーに更新SQLを実行する。
    /// </summary>
    public class PostgreSqlUpdateExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            using (var command = new NpgsqlCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<NpgsqlConnection>(element.DataSource);
                command.Transaction = ServiceContext.GetTransaction<NpgsqlTransaction>(element.DataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                int result = command.ExecuteNonQuery();
                if (!string.IsNullOrEmpty(element.ToVariable))
                {
                    ServiceContext.SharedVariable.SetValue(element.ToVariable, result);
                    Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToVariable, typeof(int));
                }
            }
            if (XSqlElementConstants.AttributeValue.commit.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.CommitTransaction(element.DataSource);
                Logger.WriteDebug(method, "コミットしました。データソース名:{0}", element.DataSource);
            }
            else if (XSqlElementConstants.AttributeValue.rollback.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.RollbackTransaction(element.DataSource);
                Logger.WriteDebug(method, "ロールバックしました。データソース名:{0}", element.DataSource);
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからSqlUpdateElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>SqlUpdateElement</returns>
        public SqlUpdateElement CreateElement(XElement rawElement)
        {
            SqlUpdateElement obj = new SqlUpdateElement();
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
