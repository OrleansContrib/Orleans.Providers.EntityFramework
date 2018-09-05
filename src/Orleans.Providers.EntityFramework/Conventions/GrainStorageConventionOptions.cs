namespace Orleans.Providers.EntityFramework.Conventions
{
    public class GrainStorageConventionOptions
    {
        public string DefaultGrainKeyPropertyName { get; set; } = "Id";

        public string DefaultGrainKeyExtPropertyName { get; set; } = "KeyExt";

        public string DefaultPersistanceCheckPropertyName { get; set; } = "Id";
    }
}