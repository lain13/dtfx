using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.Common.Configuration;

namespace IF.Batch.Common.Service
{
    /// <summary>
    /// 構成確認、初期化、実行のライフサイクルを持つサービスを定義します。
    /// </summary>
    public interface IService : IDisposable
    {
        /// <summary>
        /// 環境設定を確認します。
        /// </summary>
        /// <returns>サービスを実行できる構成の場合は <see langword="true"/>。</returns>
        bool EnsureServiceConfigurations();

        /// <summary>
        /// 初期化します。
        /// </summary>
        /// <returns>初期化に成功した場合は <see langword="true"/>。</returns>
        bool InitService();

        /// <summary>
        /// 実行します。
        /// </summary>
        void ExecuteService();
    }
}
