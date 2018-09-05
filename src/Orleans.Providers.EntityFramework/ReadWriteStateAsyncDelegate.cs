using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework
{
    internal delegate Task ReadWriteStateAsyncDelegate(string grainType, GrainReference grainReference,
        IGrainState grainState, object storageOptions);
}