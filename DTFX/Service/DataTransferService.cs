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
        private bool _disposed;

        public DataTransferContext ServiceContext { get; private set; }

        public ResultTypeCode ServiceResult { get; private set; }

        public DataTransferService()
            : this(new DataTransferContextFactory(), new ExecutorFactory())
        {
        }

        public DataTransferService(
            IDataTransferContextFactory contextFactory,
            IExecutorFactory executorFactory)
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException("contextFactory");
            }
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }

            _contextFactory = contextFactory;
            _executorFactory = executorFactory;
        }

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

        public virtual bool InitService()
        {
            ThrowIfDisposed();
            return ServiceContext != null;
        }

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
                TraceLog.WriteError(method, ex.Message);
            }
            catch
            {
                ServiceResult = ResultTypeCode.Error;
                throw;
            }
        }

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
