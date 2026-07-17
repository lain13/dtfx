/************************************************************************
* ファイル名:	OracleSelectScalarExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   28.7-001-01     2017/03/08  姜　恵遠    新規作成
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
using Oracle.ManagedDataAccess.Client;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// Oracleデータ取得処理(OracleSelectScalarExecutor)
    /// Oracleサーバーからデータを取得してその結果を変数に出力する。
    /// </summary>
    public class OracleSelectScalarExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            using (var command = new OracleCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<OracleConnection>(element.DataSource);
                command.Transaction = ServiceContext.GetTransaction<OracleTransaction>(element.DataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                var result = command.ExecuteScalar();
                if (result == DBNull.Value)
                {
                    result = null;
                }
                if (!string.IsNullOrEmpty(element.ToVariable))
                {
                    WriteToVariable(result, element);
                }
                else
                {
                    throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleSelectScalar, "XML形式が正しくありません。" + rawElement.ToString());
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからOracleSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>SqlSelectScalarElement</returns>
        public OracleSelectScalarElement CreateElement(XElement rawElement)
        {
            OracleSelectScalarElement obj = new OracleSelectScalarElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.DataSource = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.dataSource);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }

        /// <summary>
        /// DBから取得した結果を変数に出力します。
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="element">OracleSelectScalarElement</param>
        private void WriteToVariable(object obj, OracleSelectScalarElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            ServiceContext.SharedVariable.SetValue(element.ToVariable, obj);
            Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 値:{2}", element.ToVariable, obj == null ? "null" : obj.GetType().ToString(), obj);
        }
    }
}
