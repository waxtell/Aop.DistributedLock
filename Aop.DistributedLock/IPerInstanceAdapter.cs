namespace Aop.DistributedLock;

public interface IPerInstanceAdapter<T> : IDistributedLockAdapter<T> where T:class;