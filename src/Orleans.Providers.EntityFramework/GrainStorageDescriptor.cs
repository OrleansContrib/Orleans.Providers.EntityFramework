namespace Orleans.Providers.EntityFramework
{
    internal class GrainStorageDescriptor
    {
        public object GrainStorageOptions { get; set; }

        public ReadWriteStateAsyncDelegate ReadStateAsyncDelegate { get; set; }

        public ReadWriteStateAsyncDelegate WriteStateAsyncDelegate { get; set; }

        public ReadWriteStateAsyncDelegate ClearStateAsyncDelegate { get; set; }
    }
}
