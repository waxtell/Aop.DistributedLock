using System;
using System.Linq.Expressions;

namespace Aop.DistributedLock;

public interface IPerMethodAdapter<T> : IDistributedLockAdapter<T> where T : class
{
    IPerMethodAdapter<T> Lock<TReturn>(Expression<Func<T, TReturn>> target, Expectation<T>.KeyFactoryDelegate? keyFactory = null);
}