using System;
using System.Collections.Generic;
using System.Xml.Linq;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Service;
using IF.Batch.DTFX.Elements;
using IF.Batch.DTFX.Exceptions;
using IF.Batch.DTFX.Service;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 標準 Executor の登録情報と生成責務を一元管理します。
    /// </summary>
    public sealed class ExecutorFactory : IExecutorFactory
    {
        private readonly IDictionary<XName, Func<ExecutorBase>> _registrations;
        private readonly ITraceLogger _logger;

        /// <summary>
        /// 標準ロガーと標準の要素登録を使用するファクトリを生成します。
        /// </summary>
        public ExecutorFactory()
            : this(new TraceLogger())
        {
        }

        /// <summary>
        /// 指定したロガーと標準の要素登録を使用するファクトリを生成します。
        /// </summary>
        /// <param name="logger">生成したすべての Executor で共有するロガー。</param>
        public ExecutorFactory(ITraceLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            _registrations = new Dictionary<XName, Func<ExecutorBase>>
            {
                { XSqlElementConstants.ElementName.SqlSelectScalar, () => new SqlSelectScalarExecutor() },
                { XSqlElementConstants.ElementName.SqlSelect, () => new SqlSelectExecutor() },
                { XSqlElementConstants.ElementName.SqlInsert, () => new SqlInsertExecutor() },
                { XSqlElementConstants.ElementName.SqlUpdate, () => new SqlUpdateExecutor() },
                { XSqlElementConstants.ElementName.SqlDelete, () => new SqlDeleteExecutor() },
                { XSqlElementConstants.ElementName.OracleSelectScalar, () => new OracleSelectScalarExecutor() },
                { XSqlElementConstants.ElementName.OracleSelect, () => new OracleSelectExecutor() },
                { XSqlElementConstants.ElementName.OracleInsert, () => new OracleInsertExecutor() },
                { XSqlElementConstants.ElementName.OracleUpdate, () => new OracleUpdateExecutor() },
                { XSqlElementConstants.ElementName.OracleDelete, () => new OracleDeleteExecutor() },
                { XSqlElementConstants.ElementName.LocalDBSelectScalar, () => new LocalDBSelectScalarExecutor() },
                { XSqlElementConstants.ElementName.LocalDBSelect, () => new LocalDBSelectExecutor() },
                { XSqlElementConstants.ElementName.LocalDBInsert, () => new LocalDBInsertExecutor() },
                { XSqlElementConstants.ElementName.LocalDBUpdate, () => new LocalDBUpdateExecutor() },
                { XSqlElementConstants.ElementName.LocalDBDelete, () => new LocalDBDeleteExecutor() },
                { XSqlElementConstants.ElementName.If, () => new IfExecutor(this) },
                { XSqlElementConstants.ElementName.ForEach, () => new ForEachExecutor(this) },
                { XSqlElementConstants.ElementName.TraceLog, () => new TraceLogExecutor() },
                { XSqlElementConstants.ElementName.AppExit, () => new AppExitExecutor() },
                { XSqlElementConstants.ElementName.LoadCSV, () => new LoadCSVExecutor() },
                { XSqlElementConstants.ElementName.ExecuteCommand, () => new ExecuteCommandExecutor() },
                { XSqlElementConstants.ElementName.SqlServerBulkInsertFromSqlServer, () => new SqlServerBulkInsertFromSqlServerExecutor() },
                { XSqlElementConstants.ElementName.SqlServerBulkInsertFromOracle, () => new SqlServerBulkInsertFromOracleExecutor() },
                { XSqlElementConstants.ElementName.OracleBulkInsertFromOracle, () => new OracleBulkInsertFromOracleExecutor() },
                { XSqlElementConstants.ElementName.OracleBulkInsertFromSqlServer, () => new OracleBulkInsertFromSqlServerExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromSqlServer, () => new PostgreSqlBulkInsertFromSqlServerExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromOracle, () => new PostgreSqlBulkInsertFromOracleExecutor() },
                { XSqlElementConstants.ElementName.ZipArchive, () => new ZipArchiveExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlSelectScalar, () => new PostgreSqlSelectScalarExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlSelect, () => new PostgreSqlSelectExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlInsert, () => new PostgreSqlInsertExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlUpdate, () => new PostgreSqlUpdateExecutor() },
                { XSqlElementConstants.ElementName.PostgreSqlDelete, () => new PostgreSqlDeleteExecutor() }
            };
        }

        /// <summary>
        /// Application の子要素を順次実行するルート Executor を生成します。
        /// </summary>
        /// <param name="serviceContext">ジョブ全体で共有するコンテキスト。</param>
        /// <returns>共有コンテキストとロガーを設定済みのルート Executor。</returns>
        public ITaskExecutor<XElement> CreateApplicationExecutor(DataTransferContext serviceContext)
        {
            EnsureServiceContext(serviceContext);
            var executor = new ApplicationExecutor(this) { ServiceContext = serviceContext };
            executor.SetLogger(_logger);
            return executor;
        }

        /// <summary>
        /// 要素名に対応する Executor を生成し、共有コンテキストとロガーを設定します。
        /// </summary>
        /// <param name="element">実行する XML 要素。</param>
        /// <param name="serviceContext">ジョブ全体で共有するコンテキスト。</param>
        /// <returns>要素に対応する Executor。</returns>
        public ExecutorBase CreateExecutor(XElement element, DataTransferContext serviceContext)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            EnsureServiceContext(serviceContext);

            Func<ExecutorBase> createExecutor;
            if (!_registrations.TryGetValue(element.Name, out createExecutor))
            {
                throw new AppConfigurationException(
                    XSqlElementConstants.ElementName.Application,
                    "XMLを解析できませんでした。XML要素名=" + element.Name);
            }

            ExecutorBase executor = createExecutor();
            executor.ServiceContext = serviceContext;
            executor.SetLogger(_logger);
            return executor;
        }

        private static void EnsureServiceContext(DataTransferContext serviceContext)
        {
            if (serviceContext == null)
            {
                throw new ArgumentNullException("serviceContext");
            }
        }
    }
}
