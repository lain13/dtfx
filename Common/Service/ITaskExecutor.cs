using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.Common.Configuration;

namespace IF.Batch.Common.Service
{
    /// <summary>
    /// タスク実行インターフェース
    /// </summary>
    /// <typeparam name="TParameter">タスクへ渡す参照型のパラメーター。</typeparam>
    public interface ITaskExecutor<TParameter>
        where TParameter : class
    {
        /// <summary>
        /// 指定されたパラメーターでタスクを実行します。
        /// </summary>
        /// <param name="parameter">タスクの入力。</param>
        /// <returns>タスクの実行結果。</returns>
        ResultTypeCode Execute(TParameter parameter);
    }
}
