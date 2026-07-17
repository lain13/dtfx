using System.Xml.Linq;
using IF.Batch.Common.Service;
using IF.Batch.DTFX.Service;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// XML 要素に対応する Executor とルート Executor を生成します。
    /// </summary>
    public interface IExecutorFactory
    {
        /// <summary>
        /// Application の子要素を順次実行するルート Executor を生成します。
        /// </summary>
        ITaskExecutor<XElement> CreateApplicationExecutor(DataTransferContext serviceContext);

        /// <summary>
        /// 指定した XML 要素に対応する Executor を生成します。
        /// </summary>
        ExecutorBase CreateExecutor(XElement element, DataTransferContext serviceContext);
    }
}
