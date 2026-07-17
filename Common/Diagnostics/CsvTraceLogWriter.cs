using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using IF.Batch.Common.Helper;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// CSV形式でトレースログをフォーマットして出力するライタークラス
    /// </summary>
    public class CsvTraceLogWriter : ThreadSafeTraceLogWriter
    {
        #region プロパティ
        private CsvFormatter Formatter { get; set; }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// CSV 形式のトレースライターを初期化します。
        /// </summary>
        public CsvTraceLogWriter()
        {

        }
        #endregion

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="config">初期化パラメタ</param>
        public override void Initialize(ITraceLogConfiguration config)
        {
            base.Initialize(config);

            this.Formatter = new CsvFormatter(null, null, true, false);
        }
        /// <summary>
        /// CSV形式でフォーマットを行うメソッド
        /// </summary>
        /// <param name="traceEventType">トレースイベントの重大度。</param>
        /// <param name="traceDateTime">イベントが発生した日時。</param>
        /// <param name="traces">CSV の後続フィールドとして出力する値。</param>
        /// <returns>日時、重大度、スレッド ID、指定された値を含む CSV レコード。</returns>
        protected override string FormatTraceLog(TraceEventType traceEventType, DateTime traceDateTime, params string[] traces)
        {
            List<string> tokens = new List<string>();
            tokens.Add(traceDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            tokens.Add(traceEventType.ToString());
            tokens.Add(string.Format("{0:D2}", Thread.CurrentThread.ManagedThreadId));
            tokens.AddRange(traces);
            return Formatter.ToCsv(tokens).ToString();
        }
    }
}
