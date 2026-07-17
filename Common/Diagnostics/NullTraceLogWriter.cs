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
        }
    }
}
