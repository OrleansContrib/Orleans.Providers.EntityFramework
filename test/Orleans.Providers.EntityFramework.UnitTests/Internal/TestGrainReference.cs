using System;
using System.Runtime.Serialization;
using System.Text;
using Orleans.CodeGeneration;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    internal class TestGrainReference : GrainReference
    {
        internal enum Category : byte
        {
            None = 0,
            SystemTarget = 1,
            SystemGrain = 2,
            Grain = 3,
            Client = 4,
            KeyExtGrain = 6,
            GeoClient = 7,
        }

        public static TestGrainReference Create<TKey>(Entity<TKey> state)
        {
            switch (state)
            {
                case EntityWithGuidKey g:
                    return Create(g);
                case EntityWithGuidCompoundKey g:
                    return Create(g);
                case EntityWithIntegerKey g:
                    return Create(g);
                case EntityWithIntegerCompoundKey g:
                    return Create(g);
                case EntityWithStringKey g:
                    return Create(g);
                    
            }

            throw new Exception($"Unexpected type {state.GetType().Name}.");
        }

        public static TestGrainReference Create(EntityWithGuidKey state)
        {
            return Create<GrainWithGuidKey>(state.Id);
        }

        public static TestGrainReference Create<TGrain>(Guid guid)
            where TGrain : IGrainWithGuidKey
        {
            var guidBytes = guid.ToByteArray();
            var n0 = BitConverter.ToUInt64(guidBytes, 0);
            var n1 = BitConverter.ToUInt64(guidBytes, 8);
            var typeData = GetTypeDate<TGrain>();
            return Create(n0, n1, typeData, Category.Grain, null);
        }

        public static TestGrainReference Create(EntityWithGuidCompoundKey state)
        {
            return Create<GrainWithGuidCompoundKey>(state.Id, state.KeyExt);
        }

        public static TestGrainReference Create<TGrain>(Guid guid, string keyExt)
            where TGrain : IGrainWithGuidCompoundKey
        {
            var guidBytes = guid.ToByteArray();
            var n0 = BitConverter.ToUInt64(guidBytes, 0);
            var n1 = BitConverter.ToUInt64(guidBytes, 8);
            var typeData = GetTypeDate<TGrain>();
            return Create(n0, n1, typeData, Category.KeyExtGrain, keyExt);
        }

        public static TestGrainReference Create(EntityWithIntegerKey state)
        {
            return Create<GrainWithIntegerKey>(state.Id);
        }

        public static TestGrainReference Create<TGrain>(long id)
            where TGrain : IGrainWithIntegerKey
        {
            var n1 = unchecked((ulong)id);
            var typeData = GetTypeDate<TGrain>();
            return Create(0, n1, typeData, Category.Grain, null);
        }

        public static TestGrainReference Create(EntityWithIntegerCompoundKey state)
        {
            return Create<GrainWithIntegerCompoundKey>(state.Id, state.KeyExt);
        }

        public static TestGrainReference Create<TGrain>(long id, string keyExt)
            where TGrain : IGrainWithIntegerCompoundKey
        {
            var n1 = unchecked((ulong)id);
            var typeData = GetTypeDate<TGrain>();
            return Create(0, n1, typeData, Category.KeyExtGrain, keyExt);
        }

        public static TestGrainReference Create(EntityWithStringKey state)
        {
            return Create<GrainWithStringKey>(state.Id);
        }

        public static TestGrainReference Create<TGrain>(string stringKey)
            where TGrain : IGrainWithStringKey
        {
            var typeData = GetTypeDate<TGrain>();
            return Create(0, 0, typeData, Category.KeyExtGrain, stringKey);
        }

        protected static long GetTypeDate<TGrain>()
        {
            // todo: Just putting something there. not required within tests
            return typeof(TGrain).GetHashCode();
        }

        protected static TestGrainReference Create(ulong n0, ulong n1, long typeData, Category category,
            string keyExt)
        {
            var typeCodeData = ((ulong)category << 56) + ((ulong)typeData & 0x00FFFFFFFFFFFFFF);

            var s = new StringBuilder();
            s.AppendFormat("{0:x16}{1:x16}{2:x16}", n0, n1, typeCodeData);
            if (category == Category.KeyExtGrain)
            {
                s.Append("+");
                s.Append(keyExt ?? "null");
            }
            var grainId = s.ToString();


            var info = new SerializationInfo(typeof(GrainReference), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);


            info.AddValue("GrainId", grainId);
            info.AddValue("GenericArguments", "");
            return new TestGrainReference(info, context);
        }

        protected TestGrainReference(GrainReference other) : base(other)
        {
        }

        protected internal TestGrainReference(GrainReference other, InvokeMethodOptions invokeMethodOptions) : base(other, invokeMethodOptions)
        {
        }

        protected TestGrainReference(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}