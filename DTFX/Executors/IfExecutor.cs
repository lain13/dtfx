/************************************************************************
* ファイル名:	IfExecutor.cs
* 概要: 条件式を評価してTrueの場合に下位要素を実行する条件分岐処理
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
using IF.Batch.DTFX.Helper;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 条件処理(If)
    /// 条件文を評価してその結果が'True'の場合、下位要素を実行する。
    /// </summary>
    public class IfExecutor : ExecutorBase
    {
        private readonly IExecutorFactory _executorFactory;

        // JEXL式を解析する
        private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();

        public IfExecutor()
            : this(new ExecutorFactory())
        {
        }

        public IfExecutor(IExecutorFactory executorFactory)
        {
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }

            _executorFactory = executorFactory;
        }

        /// <summary>
        /// 条件式をJEXLで評価し、結果が'true'の場合に下位 XML要素を実行します。
        /// 評価結果をtoVariableで指定された共有変数に保存することも可能です。
        /// </summary>
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            string expr = element.Test;
            if (string.IsNullOrEmpty(expr))
            {
                return ResultTypeCode.Error;
            }
            // &amp;を&に置換する(XMLで&が表現できない場合のため
            expr = expr.Replace("&amp;", "&");
            string result = _evaluator.Evaluate(expr);
            TraceLog.WriteDebug(method, "結果:{0}, 計算式:{1}", result, expr);
            if (!string.IsNullOrEmpty(element.ToVariable))
            {
                ServiceContext.SharedVariable.SetValue(element.ToVariable, result);
                TraceLog.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToVariable, typeof(string));
            }
            if (result != "true")
            {
                return ResultTypeCode.Success;
            }
            var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
            return executor.Execute(element.RawElement);
        }

        /// <summary>
        /// XElementからIfElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>IfElement</returns>
        public IfElement CreateElement(XElement rawElement)
        {
            IfElement obj = new IfElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.Test = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.test);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            return obj;
        }
    }
}
