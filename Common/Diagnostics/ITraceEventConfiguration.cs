/************************************************************************
* ファイル名:	ITraceEventConfiguration.cs
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

namespace IF.Batch.Common.Diagnostics
{
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
