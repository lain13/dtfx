using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// トレースイベントを書き込む実装の共通契約を定義します。
    /// </summary>
    public interface ITraceLogWriter
    {
        /// <summary>
        /// ログを出力する
        /// </summary>
        /// <param name="level">出力するイベントの重大度。</param>
        /// <param name="trace">メソッド名やメッセージなど、ライターが整形するフィールド。</param>
        void WriteTrace(TraceEventType level, params string[] trace);
    }
}
