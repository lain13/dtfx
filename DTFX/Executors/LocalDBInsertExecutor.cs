/************************************************************************
* ファイル名:	LocalDBInsertExecutor.cs
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

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 一時DBデータ登録処理(LocalDBInsert)
    /// 一時DBに登録SQLを実行する。
    /// </summary>
    public class LocalDBInsertExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            using (var command = new SqlCommand(element.Value))
            {
                command.Connection = ServiceContext.GetLocalDB().Connection;
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                int result = command.ExecuteNonQuery();
                if (!string.IsNullOrEmpty(element.ToVariable))
                {
                    ServiceContext.SharedVariable.SetValue(element.ToVariable, result);
                    Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToVariable, typeof(int));
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからLocalDBInsertElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>LocalDBInsertElement</returns>
        public LocalDBInsertElement CreateElement(XElement rawElement)
        {
            LocalDBInsertElement obj = new LocalDBInsertElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }
    }
}
