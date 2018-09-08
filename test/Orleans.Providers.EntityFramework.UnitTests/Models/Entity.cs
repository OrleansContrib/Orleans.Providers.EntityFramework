using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Orleans.Providers.EntityFramework.UnitTests.Models
{
    public abstract class Entity<TKey> : IEquatable<Entity<TKey>>
    {
        // ReSharper disable once StaticMemberInGenericType
        protected static readonly Random Random = new Random();

        public abstract TKey Id { get; set; }

        public Guid IdGuid { get; set; } = Guid.NewGuid();

        public long IdLong { get; set; } = Random.Next();

        public string IdString { get; set; } = Random.Next().ToString();

        public string KeyExt { get; set; } = Random.Next().ToString();

        public string Title { get; set; } = Random.Next().ToString();

        public bool IsPersisted { get; set; }

        public abstract bool HasKeyExt { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Entity<TKey>);
        }

        public bool Equals(Entity<TKey> other)
        {
            return other != null &&
                   IdGuid.Equals(other.IdGuid) &&
                   IdLong == other.IdLong &&
                   IdString == other.IdString &&
                   KeyExt == other.KeyExt &&
                   Title == other.Title;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IdGuid, IdLong, IdString, KeyExt, Title);
        }

        public override string ToString()
        {
            return $"{Title} {IdGuid} {IdLong} {IdString} {KeyExt}";
        }
    }

    public class EntityWithGuidKey : Entity<Guid>
    {
        public override Guid Id { get => IdGuid; set => IdGuid = value; }
        public override bool HasKeyExt => false;
    }

    public class EntityWithGuidCompoundKey : Entity<Guid>
    {
        public override Guid Id { get => IdGuid; set => IdGuid = value; }
        public override bool HasKeyExt => true;
    }

    public class EntityWithIntegerKey : Entity<long>
    {
        public override long Id { get => IdLong; set => IdLong = value; }
        public override bool HasKeyExt => false;
    }

    public class EntityWithIntegerCompoundKey : Entity<long>
    {
        public override long Id { get => IdLong; set => IdLong = value; }
        public override bool HasKeyExt => true;
    }

    public class EntityWithStringKey : Entity<string>
    {
        public override string Id { get => IdString; set => IdString = value; }
        public override bool HasKeyExt => false;
    }

    public class EntityWithIntegerKeyWithEtag : EntityWithIntegerKey
    {
        [Timestamp]
        public byte[] ETag { get; set; } = BitConverter.GetBytes(Random.Next());


        public EntityWithIntegerKeyWithEtag Clone()
        {
            return new EntityWithIntegerKeyWithEtag
            {
                ETag = ETag.ToArray(),
                KeyExt = KeyExt,
                Title = Title,
                IdLong = IdLong,
                Id = Id,
                IdGuid = IdGuid,
                IdString = IdString,
                IsPersisted = IsPersisted
            };
        }
    }
}