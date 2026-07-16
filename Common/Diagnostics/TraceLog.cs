/************************************************************************
* ファイル名:	TraceLog.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Xml;
using System.Security.Principal;
using Microsoft.Win32;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// 監査関連プラグイン用のログクラス。
    /// 
    /// 【ソースの共通化が行われた場合は別ソリューションのプロジェクトに移動する可能性があり】
    /// </summary>
    public class TraceLog
    {
        #region シングルトン(Instance)
        protected static object _syncObj = new object();

        private static TraceLog _instance = null;

        public static TraceLog Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncObj)
                    {
                        // ダブルロック
                        if (_instance == null)
                        {
                            _instance = new TraceLog();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region プロパティ
        private ITraceLogWriter _logWriter { get; set; }
        /// <summary>
        /// ログライタ
        /// </summary>
        private ITraceLogWriter LogWriter
        {
            get
            {
                EnsureLogWriter();
                return this._logWriter;
            }
            set { this._logWriter = value; }
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TraceLog()
        {
        }
        #endregion

        /// <summary>
        /// ログ用のメソッド完全修飾名取得メソッド
        /// </summary>
        /// <param name="type">定義型</param>
        /// <param name="method">メソッド情報</param>
        /// <returns>文字列</returns>
        public static string GetMethodFqdn(MethodBase method)
        {
            return string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
        }
        /// <summary>
        /// ログライターのセットアップをプログラム的に行う
        /// </summary>
        /// <param name="writer"></param>
        public static void SetLogWriterIfNotInitialized(ITraceLogWriter writer)
        {
            if (Instance._logWriter == null)
            {
                lock (_syncObj)
                {
                    if (Instance._logWriter == null)
                    {
                        Instance._logWriter = writer;
                    }
                }
            }
        }
        /// <summary>
        /// ログライターを非NULLにする
        /// </summary>
        private void EnsureLogWriter()
        {
            if (_logWriter != null) return;

            lock (TraceLog._syncObj)
            {
                if (_logWriter != null) return;

                // デフォルトのログライターセットアップ
                AppConfigConfigurationProvider provider = new AppConfigConfigurationProvider();
                if (string.IsNullOrWhiteSpace(provider.TracePathTemplate))
                {
                    _logWriter = new NullTraceLogWriter();
                }
                else
                {
                    CsvTraceLogWriter writer = new CsvTraceLogWriter();
                    writer.Initialize(provider);
                    _logWriter = writer;
                }
            }
        }
        /// <summary>
        /// 独自項目の文字列を成形する
        /// </summary>
        /// <param name="prefix">プレフィックス</param>
        /// <param name="format">独自フォーマット</param>
        /// <param name="args">独自フォーマットの可変項目</param>
        /// <returns>成形した文字列</returns>
        private static string MessageBuildHelper(string prefix, string format, params object[] args)
        {
            if (String.IsNullOrEmpty(format) || args == null || args.Count() < 1)
            {
                return prefix;
            }
   
            return string.Format("{0} {1}", prefix, string.Format(format, args));
        }

        /// <summary>
        /// 情報メッセージを出力する
        /// </summary>
        /// <param name="method">メソッド</param>
        /// <param name="message">メッセージ</param>
        public static void WriteInfo(MethodBase method, string message)
        {
            string smethod = GetMethodFqdn(method);
            Instance.WriteTrace(TraceEventType.Information, smethod, message);
        }
        /// <summary>
        /// 情報メッセージを出力する
        /// </summary>
        /// <param name="method">メソッド</param>
        /// <param name="format">独自フォーマット</param>
        /// <param name="args">独自フォーマットの可変項目</param>
        public static void WriteInfo(MethodBase method, string format, params object[] args)
        {
            WriteInfo(method, string.Format(format, args));
        }
        /// <summary>
        /// 警告メッセージを出力する
        /// </summary>
        /// <param name="method">メソッドの完全修飾名</param>
        /// <param name="message">メッセージ</param>
        public static void WriteWarning(MethodBase method, string message)
        {
            string smethod = GetMethodFqdn(method);
            Instance.WriteTrace(TraceEventType.Warning, smethod, message);
        }
        /// <summary>
        /// 警告メッセージを出力する
        /// </summary>
        /// <param name="format">独自フォーマット</param>
        /// <param name="args">独自フォーマットの可変項目</param>
        public static void WriteWarning(MethodBase method, string format, params object[] args)
        {
            WriteWarning(method, string.Format(format, args));
        }
        /// <summary>
        /// エラーメッセージを出力する
        /// </summary>
        /// <param name="method">メソッドの完全修飾名</param>
        /// <param name="message">メッセージ</param>
        public static void WriteError(MethodBase method, string message)
        {
            string smethod = GetMethodFqdn(method);
            Instance.WriteTrace(TraceEventType.Error, smethod, message);
        }
        /// <summary>
        /// エラーメッセージを出力する
        /// </summary>
        /// <param name="format">独自フォーマット</param>
        /// <param name="args">独自フォーマットの可変項目</param>
        public static void WriteError(MethodBase method, string format, params object[] args)
        {
            WriteError(method, string.Format(format, args));
        }
        /// <summary>
        /// デバッグメッセージを出力する
        /// </summary>
        /// <param name="method">メソッドの完全修飾名</param>
        /// <param name="message">メッセージ</param>
        public static void WriteDebug(MethodBase method, string message)
        {
            string smethod = GetMethodFqdn(method);
            Instance.WriteTrace(TraceEventType.Verbose, smethod, message);
        }
        /// <summary>
        /// デバッグメッセージを出力する
        /// </summary>
        /// <param name="format">独自フォーマット</param>
        /// <param name="args">独自フォーマットの可変項目</param>
        public static void WriteDebug(MethodBase method, string format, params object[] args)
        {
            WriteDebug(method, string.Format(format, args));
        }
        /// <summary>
        /// 例外内容を出力する
        /// </summary>
        /// <param name="ex"></param>
        public static void WriteException(Exception ex)
        {
            WriteException(ex, null);
        }
        /// <summary>
        /// 例外内容を出力する
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="appendMessage"></param>
        public static void WriteException(Exception ex, string appendMessage)
        {
            Instance.WriteTrace(TraceEventType.Error, CreateTraceMessage(ex, appendMessage));
            Instance.WriteTrace(TraceEventType.Error, ex);
        }

        /// <summary>
        /// トレース出力を行う。
        /// </summary>
        /// <param name="level"></param>
        /// <param name="trace"></param>
        [Conditional("TRACE")]
        private void WriteTrace(TraceEventType level, params string[] trace)
        {
            this.LogWriter.WriteTrace(level, trace);
        }
        /// <summary>
        /// トレース出力を行う
        /// </summary>
        /// <param name="level"></param>
        /// <param name="ex"></param>
        [Conditional("TRACE")]
        private void WriteTrace(TraceEventType level, Exception ex)
        {
            this.LogWriter.WriteTrace(level, CreateExceptionTrace(ex));
        }

        /// <summary>
        /// 例外発生時の定型文字列を作成する
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="appendMessage"></param>
        public static string CreateTraceMessage(Exception ex, string appendMessage = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[ユーザー名]");
            builder.AppendLine(WindowsIdentity.GetCurrent().Name);
            builder.AppendLine();
            builder.AppendLine("[例外クラス名]");
            builder.AppendLine(ex.GetType().FullName);
            builder.AppendLine();
            builder.AppendLine("[例外の詳細情報1]");
            builder.AppendLine(ex.Source);
            builder.AppendLine();
            builder.AppendLine("[例外の詳細情報2]");
            builder.AppendLine(ex.Message);
            builder.AppendLine();
            builder.AppendLine("[例外の詳細情報3]");
            builder.AppendLine(ex.StackTrace);
            builder.AppendLine();
            builder.AppendLine("[例外の詳細情報4]");
            builder.AppendLine(ex.Source);
            builder.AppendLine();
            if (ex is System.Web.Services.Protocols.SoapException)
            {
                builder.AppendLine("[例外の詳細情報5]");
                builder.AppendLine((ex as System.Web.Services.Protocols.SoapException).Detail.InnerXml);
                builder.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(appendMessage))
            {
                builder.AppendLine("[独自項目]");
                builder.AppendLine(appendMessage);
            }
            return builder.ToString();
        }
        #region 例外メッセージ出力用ヘルパーメソッド
        /// <summary>
        /// 可能な限りすべてのプロパティデータの出力と、
        /// InnerExceptionのトレースデータを取得するメソッド
        /// </summary>
        /// <param name="ex">トレースデータを取得する例外インスタンス</param>
        /// <returns>トレース用文字列</returns>
        private string CreateExceptionTrace(Exception ex)
        {
            string template = "{0}:{1}" + Environment.NewLine;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format(template, "ExceptionType", ex.GetType().ToString()));
            builder.Append(string.Format(template, "ToString()", ex.ToString()));
            // 全てのパブリックプロパティを取得
            PropertyInfo[] properties = ex.GetType().GetProperties();
            foreach (PropertyInfo p in properties)
            {
                // InnerExceptionは最後
                if (p.Name == "InnerException") continue;

                if (p.GetGetMethod() != null)
                {
                    object o = p.GetValue(ex, null);
                    if (o == null) o = "(null)";
                    builder.Append(string.Format(template, p.Name, o.ToString()));
                    if (o is Array)
                    {
                        Array ar = o as Array;
                        foreach (object item in ar)
                        {
                            o = item;
                            if (o == null) o = "(null)";
                            builder.Append(string.Format(template, p.Name, item.ToString()));
                        }
                    }
                    else if (o is XmlNode)
                    {
                        XmlNode node = o as XmlNode;
                        builder.Append(string.Format(template, p.Name, node.InnerText));
                    }
                }
            }
            if (ex.InnerException != null)
            {
                builder.Append(string.Format(template, "InnerException", ex.InnerException.GetType().ToString()));
                builder.Append(CreateExceptionTrace(ex.InnerException));
            }

            return builder.ToString();
        }
        #endregion

    }
}
