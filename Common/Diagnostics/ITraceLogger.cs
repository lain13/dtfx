using System;
using System.Reflection;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// アプリケーションコードが使用する構造化トレースログの共通契約です。
    /// </summary>
    public interface ITraceLogger
    {
        void WriteInfo(MethodBase method, string message, params object[] args);

        void WriteWarning(MethodBase method, string message, params object[] args);

        void WriteError(MethodBase method, string message, params object[] args);

        void WriteDebug(MethodBase method, string message, params object[] args);

        void WriteException(Exception exception, string appendMessage = null);
    }
}
