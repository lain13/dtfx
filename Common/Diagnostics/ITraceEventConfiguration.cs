using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// Windows イベントログへエラーを書き込むための構成を定義します。
    /// </summary>
    public interface ITraceEventConfiguration
    {
        /// <summary>
        /// 想定外エラー時のイベントログID
        /// </summary>
        int ErrorEventId { get; }
        /// <summary>
        /// イベントソース
        /// </summary>
        string EventSource { get; }
        /// <summary>
        /// 想定外エラー時のイベントログエントリータイプ
        /// </summary>
        EventLogEntryType ErrorEventEntryType { get; }
    }
}
