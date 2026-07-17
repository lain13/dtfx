/************************************************************************
* ファイル名:	ExecuteCommandExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/10/07	姜　恵遠	新規作成
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
    /// 外部Command実行処理(ExecuteCommand)
    /// </summary>
    public class ExecuteCommandExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            if (!string.IsNullOrEmpty(element.Value))
            {
                int exitCode = ExecuteCommandSync(element);
                return exitCode == 0 ? ResultTypeCode.Success : ResultTypeCode.Error;
            }

            return ResultTypeCode.Success;
        }

        /// <summary>
        /// Commandを実行します。
        /// </summary>
        /// <param name="element"></param>
        private int ExecuteCommandSync(ExecuteCommandElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            WriteTraceLog(method, element.TraceLog, string.Format("外部コマンドを実行:cmd /c " + element.Value));
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + element.Value);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            StringBuilder standardOutput = new StringBuilder();
            StringBuilder standardError = new StringBuilder();

            using (Process proc = new Process())
            {
                proc.StartInfo = procStartInfo;
                proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
                {
                    if (args.Data != null)
                    {
                        standardOutput.AppendLine(args.Data);
                    }
                };
                proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
                {
                    if (args.Data != null)
                    {
                        standardError.AppendLine(args.Data);
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                int exitCode = proc.ExitCode;
                string result = standardOutput.ToString();
                WriteTraceLog(method, element.TraceLog, "外部コマンド結果:" + exitCode);
                WriteTraceLog(method, element.TraceLog, "外部コマンド出力内容:" + result);
                WriteTraceLog(method, element.TraceLog, "外部コマンドエラー内容:" + standardError.ToString());
                if (!string.IsNullOrEmpty(element.ToVariable))
                {
                    ServiceContext.SharedVariable.SetValue(element.ToVariable, result);
                    Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToVariable, typeof(string));
                }
                ServiceContext.SharedVariable.SetValue("exitcode", exitCode);
                Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", "exitcode", typeof(int));
                return exitCode;
            }
        }

        /// <summary>
        /// XElementからExecuteCommandElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>ExecuteCommandElement</returns>
        public ExecuteCommandElement CreateElement(XElement rawElement)
        {
            ExecuteCommandElement obj = new ExecuteCommandElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.TraceLog = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.traceLog);
            obj.Value = GetParsedStringValue(rawElement);
            if (obj.Value != null)
            {
                obj.Value = obj.Value.Trim();
            }
            return obj;
        }
    }
}
