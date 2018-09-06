namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public class GrainState<T> : IGrainState
        where T : class, new()
    {
        public T State;
        object IGrainState.State
        {
            get => State;
            set => State = value as T;
        }

        public string ETag { get; set; }
    }
}