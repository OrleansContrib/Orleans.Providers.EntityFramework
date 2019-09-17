namespace Orleans.Providers.EntityFramework.UnitTests.Models
{
    public class GrainStateWrapper<T>
    {
        public T Value { get; set; }
        
    }
}