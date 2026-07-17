namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// ジョブ実行ごとの DataTransferContext を生成します。
    /// </summary>
    public interface IDataTransferContextFactory
    {
        /// <summary>
        /// 現在のアプリケーション設定からコンテキストを生成します。
        /// </summary>
        /// <param name="context">生成されたコンテキスト。</param>
        /// <returns>実行可能なコンテキストを生成できた場合は <see langword="true"/>。</returns>
        bool TryCreate(out DataTransferContext context);
    }
}
