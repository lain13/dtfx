using System;
using System.Reflection;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// 既存の静的 TraceLog API を ITraceLogger として公開します。
    /// </summary>
    public sealed class TraceLogger : ITraceLogger
    {
        public void WriteInfo(MethodBase method, string message, params object[] args)
        {
            if (HasArguments(args))
            {
                TraceLog.WriteInfo(method, message, args);
                return;
            }
            TraceLog.WriteInfo(method, message);
        }

        public void WriteWarning(MethodBase method, string message, params object[] args)
        {
            if (HasArguments(args))
            {
                TraceLog.WriteWarning(method, message, args);
                return;
            }
            TraceLog.WriteWarning(method, message);
        }

        public void WriteError(MethodBase method, string message, params object[] args)
        {
            if (HasArguments(args))
            {
                TraceLog.WriteError(method, message, args);
                return;
            }
            TraceLog.WriteError(method, message);
        }

        public void WriteDebug(MethodBase method, string message, params object[] args)
        {
            if (HasArguments(args))
            {
                TraceLog.WriteDebug(method, message, args);
                return;
            }
            TraceLog.WriteDebug(method, message);
        }

        public void WriteException(Exception exception, string appendMessage = null)
        {
            TraceLog.WriteException(exception, appendMessage);
        }

        private static bool HasArguments(object[] args)
        {
            return args != null && args.Length > 0;
        }
    }
}
