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
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 終了処理(AppExit)
    /// 現在実行中のサブ機能を終了する。
    /// </summary>
    public class AppExitExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            AppExitElement element = CreateElement(rawElement);
            if (!string.IsNullOrWhiteSpace(element.Value))
            {
                Logger.WriteInfo(method, element.Value);
            }
            throw new AppExitException(element);
        }

        /// <summary>
        /// XElementからAppExitElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>解析した <see cref="AppExitElement"/>。</returns>
        public AppExitElement CreateElement(XElement rawElement)
        {
            AppExitElement obj = new AppExitElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.Result = GetIntValue(rawElement, XSqlElementConstants.AttributeName.result, (int)ResultTypeCode.Success).Value;
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }
    }
}
