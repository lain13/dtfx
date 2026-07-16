/************************************************************************
* ファイル名:	CsvTraceLogWriter.cs
* 概要: CSV形式でトレースログを出力するライタークラス
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
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
        /// <param name="traceEventType"></param>
        /// <param name="traceDateTime"></param>
        /// <param name="traces"></param>
        /// <returns></returns>
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
