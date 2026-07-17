using System;
using System.Reflection;
using IF.Batch.Common.Configuration;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Service;
using IF.Batch.DTFX.Exceptions;
using IF.Batch.DTFX.Executors;

namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// データ転送ジョブの構成確認、初期化、実行、終了処理を調整します。
    /// </summary>
    public class DataTransferService : IService
    {
        private readonly IDataTransferContextFactory _contextFactory;
        private readonly IExecutorFactory _executorFactory;
        private readonly ITraceLogger _logger;
        private bool _disposed;

        /// <summary>
        /// 現在のジョブ実行コンテキストを取得します。構成前は <see langword="null"/> です。
        /// </summary>
        public DataTransferContext ServiceContext { get; private set; }

        /// <summary>
        /// 最後に実行したジョブの集約結果を取得します。
        /// </summary>
        public ResultTypeCode ServiceResult { get; private set; }

        /// <summary>
        /// 標準の構成ファクトリ、Executor ファクトリ、ロガーを使用します。
        /// </summary>
        public DataTransferService()
            : this(new TraceLogger())
        {
        }

        /// <summary>
        /// 指定したロガーをすべての標準コンポーネントで共有します。
        /// </summary>
        /// <param name="logger">ジョブ全体で共有するロガー。</param>
        public DataTransferService(ITraceLogger logger)
            : this(new DataTransferContextFactory(logger), new ExecutorFactory(logger), logger)
        {
        }

        /// <summary>
        /// 指定したファクトリと標準ロガーを使用します。
        /// </summary>
        /// <param name="contextFactory">ジョブ実行コンテキストを生成するファクトリ。</param>
        /// <param name="executorFactory">XML 要素に対応する Executor を生成するファクトリ。</param>
        public DataTransferService(
            IDataTransferContextFactory contextFactory,
            IExecutorFactory executorFactory)
            : this(contextFactory, executorFactory, new TraceLogger())
        {
        }

        /// <summary>
        /// 構成、Executor、ログの依存関係を明示してサービスを生成します。
        /// </summary>
        /// <param name="contextFactory">ジョブ実行コンテキストを生成するファクトリ。</param>
        /// <param name="executorFactory">XML 要素に対応する Executor を生成するファクトリ。</param>
        /// <param name="logger">ジョブ全体で共有するロガー。</param>
        public DataTransferService(
            IDataTransferContextFactory contextFactory,
            IExecutorFactory executorFactory,
            ITraceLogger logger)
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException("contextFactory");
            }
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _contextFactory = contextFactory;
            _executorFactory = executorFactory;
            _logger = logger;
        }

        /// <summary>
        /// ジョブ設定と XML 定義を読み込み、実行コンテキストを構成します。
        /// </summary>
        /// <returns>実行可能なコンテキストを生成できた場合は <see langword="true"/>。</returns>
        public virtual bool EnsureServiceConfigurations()
        {
            ThrowIfDisposed();

            DataTransferContext context;
            bool configured = _contextFactory.TryCreate(out context);
            if (configured && context == null)
            {
                throw new InvalidOperationException(
                    "The context factory returned success without a context.");
            }

            ServiceContext = context;
            return configured;
        }

        /// <summary>
        /// 構成済みコンテキストが存在することを確認します。
        /// </summary>
        /// <returns>初期化済みのコンテキストがある場合は <see langword="true"/>。</returns>
        public virtual bool InitService()
        {
            ThrowIfDisposed();
            return ServiceContext != null;
        }

        /// <summary>
        /// 構成済みの Application XML を実行し、結果コードを保存します。
        /// </summary>
        public void ExecuteService()
        {
            ThrowIfDisposed();
            if (ServiceContext == null || ServiceContext.RootElement == null)
            {
                throw new InvalidOperationException(
                    "Service configuration must be completed before execution.");
            }

            MethodBase method = MethodInfo.GetCurrentMethod();
            try
            {
                var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
                ServiceResult = executor.Execute(ServiceContext.RootElement);
            }
            catch (AppExitException ex)
            {
                ServiceResult = (ResultTypeCode)ex.Element.Result;
            }
            catch (AppConfigurationException ex)
            {
                ServiceResult = ResultTypeCode.Error;
                _logger.WriteError(method, ex.Message);
            }
            catch
            {
                ServiceResult = ResultTypeCode.Error;
                throw;
            }
        }

        /// <summary>
        /// エラー結果ではロールバックし、成功または警告結果ではコミットしてから接続を破棄します。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (ServiceContext == null)
            {
                return;
            }

            bool commit = ServiceResult != ResultTypeCode.Error;
            ServiceContext.DisposeTransactions(commit);
            ServiceContext.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
