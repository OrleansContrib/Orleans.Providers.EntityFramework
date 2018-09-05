using System;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public delegate ValueType GetCompoundKeyDelegate(IAddressable addressable, out string keyExt);
}