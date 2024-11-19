using System;
using System.Threading.Tasks;

namespace Aop.DistributedLock.Unit.Tests;

public interface IForTestingPurposes
{
    int MaxSimultaneousCalls { get; }
    int RunningCount { get; }
    string MethodCall(int arg1, string arg2);
}

public class ForTestingPurposes : IForTestingPurposes
{
    public int MaxSimultaneousCalls { get; internal set; }
    public int RunningCount { get; internal set; }

    public string MethodCall(int arg1, string arg2)
    {
        MaxSimultaneousCalls = Math.Max(MaxSimultaneousCalls, ++RunningCount);
        Task.Delay(2000).Wait();
        RunningCount--;
        return arg1 + arg2;
    }
}