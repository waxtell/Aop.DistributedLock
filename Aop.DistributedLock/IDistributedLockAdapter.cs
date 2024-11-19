namespace Aop.DistributedLock;

public interface IDistributedLockAdapter<T> where T : class
{
    T Adapt(T instance);
}