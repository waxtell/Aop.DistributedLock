using System.Collections.Generic;
using System.Reflection;
using Castle.DynamicProxy;
using Medallion.Threading;

namespace Aop.DistributedLock;

public abstract class BaseAdapter<T>(IDistributedLockProvider lockProvider) : IInterceptor
    where T : class
{
    protected readonly List<Expectation<T>> Expectations = [];

    protected IDistributedLockProvider LockProvider = lockProvider;

    public T Adapt(T instance)
    {
        return typeof(T).GetTypeInfo().IsInterface
            ? new ProxyGenerator().CreateInterfaceProxyWithTarget(instance, this)
            : new ProxyGenerator().CreateClassProxyWithTarget(instance, this);
    }

    public abstract void Intercept(IInvocation invocation);
}