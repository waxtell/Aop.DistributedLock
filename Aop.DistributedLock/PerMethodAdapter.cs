using System;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using Medallion.Threading;

namespace Aop.DistributedLock;

public class PerMethodAdapter<T>(IDistributedLockProvider cacheImplementation)
    : BaseAdapter<T>(cacheImplementation), IPerMethodAdapter<T>
    where T : class
{
    public override void Intercept(IInvocation invocation)
    {
        var lockKey = Expectations
                        .FirstOrDefault(x => x.IsHit(invocation))
                        ?.GetLockKey(invocation);

        if (lockKey != null)
        {
            var @lock = LockProvider.CreateLock(lockKey);

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

    public IPerMethodAdapter<T> Lock<TReturn>(Expression<Func<T, TReturn>> target, Expectation<T>.KeyFactoryDelegate? keyFactory = null)
    {
        MethodCallExpression? expression = null;

        switch (target.Body)
        {
            case MemberExpression memberExpression:
                Lock(memberExpression, keyFactory);
                return this;

            case UnaryExpression unaryExpression:
                expression = unaryExpression.Operand as MethodCallExpression;
                break;
        }

        expression ??= target.Body as MethodCallExpression;

        Lock(expression!, keyFactory);

        return this;
    }

    private void Lock(MemberExpression expression, Expectation<T>.KeyFactoryDelegate? keyFactory)
    {
        Expectations
            .Add(Expectation<T>.FromMemberAccessExpression(expression, keyFactory));
    }

    private void Lock(MethodCallExpression expression, Expectation<T>.KeyFactoryDelegate? keyFactory)
    {
        Expectations
            .Add
            (
                Expectation<T>
                    .FromMethodCallExpression
                    (
                        expression,
                        keyFactory
                    )
            );
    }
}