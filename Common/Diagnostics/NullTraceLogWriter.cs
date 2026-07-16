/************************************************************************
* ファイル名:	NullTraceLogWriter.cs
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

namespace IF.Batch.Common.Diagnostics
{
    class NullTraceLogWriter : ITraceLogWriter
    {
        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NullTraceLogWriter() { }
        #endregion

        public void WriteTrace(System.Diagnostics.TraceEventType level, params string[] trace)
        {
            // Do Nothing
        }
    }
}
