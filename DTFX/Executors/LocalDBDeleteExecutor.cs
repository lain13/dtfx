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
    /// 一時DBデータ削除処理(LocalDBDelete)
    /// 一時DBに削除SQLを実行する。
    /// </summary>
    public class LocalDBDeleteExecutor : ExecutorBase
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
        /// XElementからLocalDBDeleteElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>LocalDBDeleteElement</returns>
        public LocalDBDeleteElement CreateElement(XElement rawElement)
        {
            LocalDBDeleteElement obj = new LocalDBDeleteElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }
    }
}
