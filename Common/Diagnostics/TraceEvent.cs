using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// イベントログ作成用クラス
    /// </summary>
    public class TraceEvent
    {
        #region プロパティ
        private static int _infoEventId = 1;
        /// <summary>
        /// 情報レベルのイベントIDの既定値:1
        /// </summary>
        public static int DefaultInfoEventId
        {
            get { return _infoEventId; }
            set { TraceEvent._infoEventId = value; }
        }
        private static int _warningEventId = 1;
        /// <summary>
        /// 警告レベルのイベントIDの既定値:1
        /// </summary>
        public static int DefaultWarningEventId
        {
            get { return _warningEventId; }
            set { TraceEvent._warningEventId = value; }
        }
        private static int _criticalEventId = 1;
        /// <summary>
        /// クリティカルエラーレベルのイベントIDの既定値:1
        /// </summary>
        public static int DefaultCriticalEventId
        {
            get { return _criticalEventId; }
            set { TraceEvent._criticalEventId = value; }
        }
        private static string _defaultEventSource = "EventSource";
        /// <summary>
        /// トレースソース既定値
        /// </summary>
        public static string DefaultEventSource
        {
            get { return _defaultEventSource; }
            set { TraceEvent._defaultEventSource = value; }
        }
        #endregion

        #region スタティックコンストラクタ
        /// <summary>
        /// スタティックコンストラクタ
        /// </summary>
        static TraceEvent()
        {
            // 初期動作として、構成ファイルのプロバイダからトレースソースを設定する
            ITraceEventConfiguration provider = new AppConfigConfigurationProvider();
            TraceEvent.DefaultEventSource = provider.EventSource;
        }
        #endregion
        /// <summary>
        /// 既定値を使用してクリティカルエラーイベントログエントリ作成
        /// </summary>
        /// <param name="message">メッセージ</param>
        public static void WriteCritical(string message)
        {
            TraceEvent.WriteCritical(message, TraceEvent.DefaultCriticalEventId);
        }
        /// <summary>
        /// 引数で指定された項目以外は既定値を使用して
        /// エラーイベントログエントリ作成
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="eventId">イベントID</param>
        public static void WriteCritical(string message, int eventId)
        {
            TraceEvent.WriteEvent(message, eventId, EventLogEntryType.Error, TraceEvent.DefaultEventSource);
        }
        /// <summary>
        /// 既定値を使用してクリティカルエラーイベントログエントリ作成
        /// </summary>
        /// <param name="ex">例外</param>
        public static void WriteCritical(Exception ex)
        {
            TraceEvent.WriteCritical(ex, null);
        }
        /// <summary>
        /// 既定値を使用してクリティカルエラーイベントログエントリ作成
        /// </summary>
        /// <param name="ex">例外</param>
        /// <param name="appendMessage">独自メッセージ</param>
        public static void WriteCritical(Exception ex, string appendMessage)
        {
            TraceEvent.WriteCritical(ex, appendMessage, TraceEvent.DefaultCriticalEventId);
        }
        /// <summary>
        /// 引数で指定された項目以外、既定値を使用して
        /// クリティカルエラーイベントログエントリ作成
        /// </summary>
        /// <param name="ex">例外</param>
        /// <param name="appendMessage">独自メッセージ</param>
        /// <param name="eventId">イベントID</param>
        public static void WriteCritical(Exception ex, string appendMessage, int eventId)
        {
            TraceEvent.WriteCritical(TraceLog.CreateTraceMessage(ex, appendMessage), eventId);
        }
        /// <summary>
        /// 既定値を使用してワーニングイベントログエントリ作成
        /// </summary>
        /// <param name="message">メッセージ</param>
        public static void WriteWarn(string message)
        {
            TraceEvent.WriteWarn(message, TraceEvent.DefaultWarningEventId);
        }
        /// <summary>
        /// 引数で指定された項目以外は既定値を使用して
        /// 情報イベントログエントリを作成する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="eventId">イベントID</param>
        public static void WriteWarn(string message, int eventId)
        {
            TraceEvent.WriteEvent(message, eventId, EventLogEntryType.Warning, TraceEvent.DefaultEventSource);
        }
        /// <summary>
        /// 既定値を使用してインフォメーションイベントログエントリ作成
        /// </summary>
        /// <param name="message">メッセージ</param>
        public static void WriteInfo(string message)
        {
            TraceEvent.WriteInfo(message, TraceEvent.DefaultInfoEventId);
        }
        /// <summary>
        /// 引数で指定された項目以外は既定値を使用して
        /// 情報イベントログエントリを作成する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="eventId">イベントID</param>
        public static void WriteInfo(string message,  int eventId)
        {
            TraceEvent.WriteEvent(message, eventId, EventLogEntryType.Information);
        }
        /// <summary>
        /// 既定のイベントソースを使用して、イベントログエントリを作成する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="eventId">イベントID</param>
        /// <param name="eventLogEntryType">ログの種類</param>
        public static void WriteEvent(string message, int eventId, EventLogEntryType eventLogEntryType)
        {
            TraceEvent.WriteEvent(message, eventId, eventLogEntryType, TraceEvent.DefaultEventSource);
        }
        /// <summary>
        /// イベントログエントリを作成するメソッド
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="eventId">イベントID</param>
        /// <param name="eventLogEntryType">イベントログエントリの種類</param>
        /// <param name="eventSource">イベントソース</param>
        public static void WriteEvent(string message, int eventId, EventLogEntryType eventLogEntryType, string eventSource)
        {
            EventLog.WriteEntry(eventSource, message, eventLogEntryType, eventId);
        }
        /// <summary>
        /// 例外クラスを引数にとる、イベントログエントリを作成するメソッド
        /// のオーバーロード.
        /// 既定のイベントソースを使用して、イベントログのエントリを作成する
        /// </summary>
        /// <param name="ex">例外</param>
        /// <param name="appendMessage">独自メッセージ項目</param>
        /// <param name="eventId">イベントID</param>
        /// <param name="eventLogEntryType">イベントログエントリの種類</param>
        public static void WriteEvent(Exception ex, string appendMessage, int eventId, EventLogEntryType eventLogEntryType)
        {
            TraceEvent.WriteEvent(TraceLog.CreateTraceMessage(ex, appendMessage), eventId, eventLogEntryType, TraceEvent.DefaultEventSource);
        }
        /// <summary>
        /// 例外クラスを引数にとる、イベントログエントリを作成するメソッド
        /// のオーバーロード
        /// </summary>
        /// <param name="ex">例外</param>
        /// <param name="appendMessage">独自メッセージ項目</param>
        /// <param name="eventId">イベントID</param>
        /// <param name="eventLogEntryType">イベントログエントリの種類</param>
        /// <param name="eventSource">イベントソース</param>
        public static void WriteEvent(Exception ex, string appendMessage, int eventId, EventLogEntryType eventLogEntryType, string eventSource)
        {
            TraceEvent.WriteEvent(TraceLog.CreateTraceMessage(ex, appendMessage), eventId, eventLogEntryType, eventSource);
        }
    }
}
