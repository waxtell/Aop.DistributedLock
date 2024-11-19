using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Xunit;

namespace Aop.DistributedLock.Unit.Tests;

public class PerMethodAdapterTests
{
    [Fact]
    public async Task MultipleInvocationsAreProcessedSerially()
    {
        var instance = new ForTestingPurposes();
        var proxy = new PerMethodAdapter<IForTestingPurposes>(DistributedLockProviderFactory())
            .Lock(x => x.MethodCall(It.IsAny<int>(), "zero"))
            .Adapt(instance);

        var tasks = Enumerable.Range(1, 5).Select(_ => Task.Run(() =>
        {
            var result = proxy.MethodCall(0, "zero");
            return result;
        }));

        await Task.WhenAll(tasks);
        Assert.Equal(1, instance.MaxSimultaneousCalls);
    }

    public static IDistributedLockProvider DistributedLockProviderFactory()
    {
        return
            new FileDistributedSynchronizationProvider(new DirectoryInfo(Environment.CurrentDirectory));
    }
}