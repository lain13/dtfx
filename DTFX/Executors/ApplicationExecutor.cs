using System;
using System.Xml.Linq;
using IF.Batch.Common.Configuration;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// Application 要素の子要素を順番に実行します。
    /// </summary>
    public class ApplicationExecutor : ExecutorBase
    {
        private readonly IExecutorFactory _executorFactory;

        /// <summary>
        /// 標準の Executor 構成を使用してインスタンスを生成します。
        /// </summary>
        public ApplicationExecutor()
            : this(new ExecutorFactory())
        {
        }

        /// <summary>
        /// 指定したファクトリを使用してインスタンスを生成します。
        /// </summary>
        public ApplicationExecutor(IExecutorFactory executorFactory)
        {
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }

            _executorFactory = executorFactory;
        }

        /// <summary>
        /// XML ルート要素の子要素を順次実行し、結果を集約します。
        /// </summary>
        public override ResultTypeCode Execute(XElement rawElement)
        {
            if (rawElement == null)
            {
                throw new ArgumentNullException("rawElement");
            }

            ResultTypeCode result = ResultTypeCode.Success;
            foreach (XElement element in rawElement.Elements())
            {
                ExecutorBase executor = CreateExecutor(element);
                result = MergeResultTypeCode(result, executor.Execute(element));
            }

            return result;
        }

        /// <summary>
        /// XML 要素に対応する Executor を生成します。
        /// </summary>
        protected virtual ExecutorBase CreateExecutor(XElement element)
        {
            return _executorFactory.CreateExecutor(element, ServiceContext);
        }
    }
}
