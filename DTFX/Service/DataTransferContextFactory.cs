namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// DataTransferContext の標準ファクトリです。
    /// </summary>
    public sealed class DataTransferContextFactory : IDataTransferContextFactory
    {
        public DataTransferContext Create()
        {
            return new DataTransferContext();
        }
    }
}
