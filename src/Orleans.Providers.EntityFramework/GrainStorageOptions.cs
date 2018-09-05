using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework
{
    public class GrainStorageOptions<TContext, TGrainState>
        where TContext : DbContext
    {
        internal Func<TContext, IQueryable<TGrainState>> ReadQuery { get; set; }

        internal Func<IAddressable, Expression<Func<TGrainState, bool>>> QueryExpressionGeneratorFunc { get; set; }

        internal Func<TGrainState, bool> IsPersistedFunc { get; set; }
    }
}