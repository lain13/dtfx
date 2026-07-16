/************************************************************************
* ファイル名:	ITraceLogWriter.cs
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
    public interface ITraceLogWriter
    {
        /// <summary>
        /// ログを出力する
        /// </summary>
        /// <param name="level"></param>
        /// <param name="trace"></param>
        void WriteTrace(TraceEventType level, params string[] trace);
    }
}
