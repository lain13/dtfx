namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// ジョブ実行ごとの DataTransferContext を生成します。
    /// </summary>
    public interface IDataTransferContextFactory
    {
        DataTransferContext Create();
    }
}
