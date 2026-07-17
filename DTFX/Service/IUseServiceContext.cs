using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.Common.Configuration;

namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// 実行時のサービスコンテキストを受け取るコンポーネントを定義します。
    /// </summary>
    /// <typeparam name="TContext">サービスコンテキストの型</typeparam>
    public interface IUseServiceContext<TContext>
    {
        /// <summary>
        /// コンポーネントが使用するサービスコンテキストを取得または設定します。
        /// </summary>
        TContext ServiceContext { get; set; }
    }
}
