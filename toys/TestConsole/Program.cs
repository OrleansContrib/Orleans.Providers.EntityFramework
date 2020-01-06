using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestConsole
{
    class Program
    {
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<TestEntity> Entities { get; set; }
        }

        public class TestEntity
        {
            public Guid Id { get; set; }
        }

        static void Main(string[] args)
        {
            var sp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TestDbContext>(builder => builder
                    .UseInMemoryDatabase("test")
                    .EnableSensitiveDataLogging())
                .BuildServiceProvider();

            var predicate = CreatePredicate<TestEntity, Guid>("__id");
            var idParameter = Expression.Parameter(typeof(Guid), "id");

            Expression<Func<TestDbContext, IQueryable<TestEntity>>> queryRootExpression
                = (ctx) => ctx.Entities.AsQueryable();
            var queryable = queryRootExpression.Body;
            var compiledLambdaBody = Expression.Call(
                typeof(Queryable).GetMethods().Single(mi =>
                        mi.Name == nameof(Queryable.SingleOrDefault) && mi.GetParameters().Count() == 2)
                    .MakeGenericMethod(typeof(TestEntity)),
                queryable,
                Expression.Quote(predicate));
            var lambdaExpression = Expression.Lambda<Func<TestDbContext, Guid, TestEntity>>(
                compiledLambdaBody, queryRootExpression.Parameters[0], idParameter);
            var failingQuery = EF.CompileQuery(lambdaExpression);

            var okQuery = EF.CompileQuery((TestDbContext c, Guid id)
               => c.Entities
                   .Where(e => e.Id == id) // removing this results in an exception
                   .SingleOrDefault(predicate)
               );

            //            var failingQuery = EF.CompileQuery((TestDbContext c, Guid id)
            //               => c.Entities
            //                   .SingleOrDefault(predicate)
            //               );

            var context = sp.GetRequiredService<TestDbContext>();
            var entity = new TestEntity() { Id = Guid.NewGuid() };
            context.Add(entity);
            context.SaveChanges();

            // works fine
            okQuery(context, entity.Id);

            // fails: System.Collections.Generic.KeyNotFoundException: The given key '__id' was not present in the dictionary.
            TestEntity r = failingQuery(context, entity.Id);
            Console.WriteLine(r);

        }

        private static Expression<Func<TEntity, bool>> CreatePredicate<TEntity, TKey>(
                string grainKeyParamName = "__id")
        {
            // Creates the exact expression as (e => e.Id == id)
            ParameterExpression stateParam = Expression.Parameter(typeof(TEntity), "state");
            ParameterExpression grainKeyParam = Expression.Parameter(typeof(TKey), grainKeyParamName);
            MemberExpression stateKeyParam = Expression.Property(stateParam, "Id");

            BinaryExpression equals = Expression.Equal(grainKeyParam, stateKeyParam);

            return Expression.Lambda<Func<TEntity, bool>>(equals, stateParam);
        }
    }
}
