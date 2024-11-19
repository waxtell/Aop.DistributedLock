using System.Linq;
using Castle.DynamicProxy;
using Medallion.Threading;

namespace Aop.DistributedLock;

public class PerInstanceAdapter<T>(IDistributedLockProvider lockProvider)
    : BaseAdapter<T>(lockProvider), IPerInstanceAdapter<T>
    where T : class
{
    public override void Intercept(IInvocation invocation)
    {
        var expectation = Expectations.FirstOrDefault(x => x.IsHit(invocation));

        var cacheKey = expectation?.GetLockKey(invocation);

        if (cacheKey != null)
        {
            var @lock = LockProvider.CreateLock(cacheKey);

            using (@lock.Acquire())
            {
                invocation.Proceed();
            }
        }
        else
        {
            invocation.Proceed();
        }
    }
}