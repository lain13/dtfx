using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IF.Batch.Common.Helper;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// Serilog を使用して従来互換の CSV トレースログを出力します。
    /// </summary>
    public sealed class SerilogTraceLogWriter : ITraceLogWriter, IDisposable
    {
        private Logger _logger;
        private bool _enabled;
        private bool _disposed;

        /// <summary>
        /// 出力先、ログレベル、ローテーション、文字エンコーディングを設定します。
        /// </summary>
        /// <param name="config">トレースログの構成。</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> が <see langword="null"/> です。</exception>
        /// <exception cref="ArgumentException">ログファイルのパスが空です。</exception>
        public void Initialize(ITraceLogConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (string.IsNullOrWhiteSpace(config.TracePathTemplate))
            {
                throw new ArgumentException("A trace path is required.", "config");
            }

            string path = FileHelper.ResolvePathFromTemplate(config.TracePathTemplate, DateTime.Now);
            if (!config.Append)
            {
                path = FileHelper.NextFileName(path);
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            long? sizeLimit = config.MaxSize > 0 ? (long?)config.MaxSize : 10L * 1024 * 1024;
            var levelSwitch = new LoggingLevelSwitch(ToMinimumLevel(config.TraceSourceLevels));
            _enabled = config.TraceSourceLevels != SourceLevels.Off;
            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File(
                    new SerilogCsvFormatter(),
                    path,
                    fileSizeLimitBytes: sizeLimit,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: null,
                    buffered: !config.AutoFlush,
                    encoding: config.Encoding ?? System.Text.Encoding.Default)
                .CreateLogger();

            AppDomain.CurrentDomain.DomainUnload += CloseOnExit;
            AppDomain.CurrentDomain.ProcessExit += CloseOnExit;
        }

        /// <summary>
        /// 指定された重大度でトレースイベントを書き込みます。
        /// </summary>
        /// <param name="level">トレースイベントの重大度。</param>
        /// <param name="trace">メソッド名とメッセージを含むトレースフィールド。</param>
        public void WriteTrace(TraceEventType level, params string[] trace)
        {
            if (_logger == null || _disposed || !_enabled)
            {
                return;
            }

            string method = trace != null && trace.Length > 1 ? trace[0] : null;
            string message = trace == null || trace.Length == 0 ? string.Empty : trace[trace.Length - 1];
            _logger
                .ForContext("ThreadId", Thread.CurrentThread.ManagedThreadId)
                .ForContext("Method", method)
                .Write(ToLogEventLevel(level), "{TraceMessage:l}", message);
        }

        /// <summary>
        /// 例外と任意の補足メッセージをエラーレベルで書き込みます。
        /// </summary>
        /// <param name="exception">出力する例外。<see langword="null"/> の場合は何も出力しません。</param>
        /// <param name="appendMessage">例外メッセージの代わりに出力する補足メッセージ。</param>
        public void WriteException(Exception exception, string appendMessage)
        {
            if (_logger == null || _disposed || !_enabled || exception == null)
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(appendMessage)
                ? exception.Message
                : appendMessage;
            _logger
                .ForContext("ThreadId", Thread.CurrentThread.ManagedThreadId)
                .Error(exception, "{TraceMessage:l}", message);
        }

        /// <summary>
        /// バッファーをフラッシュし、ロガーが使用するリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AppDomain.CurrentDomain.DomainUnload -= CloseOnExit;
            AppDomain.CurrentDomain.ProcessExit -= CloseOnExit;
            if (_logger != null)
            {
                _logger.Dispose();
                _logger = null;
            }
        }

        private void CloseOnExit(object sender, EventArgs args)
        {
            Dispose();
        }

        private static LogEventLevel ToMinimumLevel(SourceLevels level)
        {
            switch (level)
            {
                case SourceLevels.Off:
                    return LogEventLevel.Fatal;
                case SourceLevels.Critical:
                    return LogEventLevel.Fatal;
                case SourceLevels.Error:
                    return LogEventLevel.Error;
                case SourceLevels.Warning:
                    return LogEventLevel.Warning;
                case SourceLevels.Information:
                    return LogEventLevel.Information;
                case SourceLevels.Verbose:
                    return LogEventLevel.Debug;
                default:
                    return LogEventLevel.Verbose;
            }
        }

        private static LogEventLevel ToLogEventLevel(TraceEventType level)
        {
            switch (level)
            {
                case TraceEventType.Critical:
                    return LogEventLevel.Fatal;
                case TraceEventType.Error:
                    return LogEventLevel.Error;
                case TraceEventType.Warning:
                    return LogEventLevel.Warning;
                case TraceEventType.Information:
                    return LogEventLevel.Information;
                default:
                    return LogEventLevel.Debug;
            }
        }
    }
}
