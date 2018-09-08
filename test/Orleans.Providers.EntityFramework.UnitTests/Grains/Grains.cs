using Orleans.Providers.EntityFramework.UnitTests.Models;

namespace Orleans.Providers.EntityFramework.UnitTests.Grains
{
    public class GrainWithGuidKey : Grain<EntityWithGuidKey>, IGrainWithGuidKey { }
    public class GrainWithIntegerKey : Grain<EntityWithIntegerKey>, IGrainWithIntegerKey { }
    public class GrainWithStringKey : Grain<EntityWithStringKey>, IGrainWithStringKey { }

    public class GrainWithGuidCompoundKey : Grain<EntityWithGuidCompoundKey>, IGrainWithGuidCompoundKey { }
    public class GrainWithIntegerCompoundKey : Grain<EntityWithIntegerCompoundKey>, IGrainWithIntegerCompoundKey { }

    public class GrainWithIntegerKeyWithEtag : Grain<EntityWithIntegerKeyWithEtag>, IGrainWithIntegerKey { }
}