using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Processes;

namespace TLinkProxy.Tests.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class ProcessDiagnosticsMonitorTests
{
    [TestMethod]
    public async Task StartAsync_SamplesSnapshotAndUsesConfiguredInterval()
    {
        ISettingsService settings = Substitute.For<ISettingsService>();
        settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns("5");
        ILlamaCppProcessManager processManager = Substitute.For<ILlamaCppProcessManager>();
        processManager.GetProcessMemory().Returns([new ProcessMemorySample { Alias = "qwen", ProcessId = 42, WorkingSetBytes = 1024 }]);

        using ProcessDiagnosticsMonitor sut = new(settings, processManager);

        await sut.StartAsync(CancellationToken.None);

        ProcessDiagnosticsSnapshot snapshot = await WaitForProcessesAsync(sut);
        await sut.StopAsync(CancellationToken.None);

        Assert.AreEqual(TimeSpan.FromSeconds(5), sut.Interval);
        Assert.IsGreaterThan(0, snapshot.WorkingSetBytes);
        Assert.IsGreaterThan(0, snapshot.ThreadPoolThreads);
        Assert.HasCount(1, snapshot.Processes);
        Assert.AreEqual("qwen", snapshot.Processes[0].Alias);
    }

    [TestMethod]
    public async Task StartAsync_MissingSetting_UsesDefaultInterval()
    {
        ISettingsService settings = Substitute.For<ISettingsService>();
        settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns((string)null);
        ILlamaCppProcessManager processManager = Substitute.For<ILlamaCppProcessManager>();
        processManager.GetProcessMemory().Returns([new ProcessMemorySample { Alias = "a" }]);

        using ProcessDiagnosticsMonitor sut = new(settings, processManager);

        await sut.StartAsync(CancellationToken.None);
        await WaitForProcessesAsync(sut);
        await sut.StopAsync(CancellationToken.None);

        Assert.AreEqual(TimeSpan.FromSeconds(Constants.DefaultDiagnosticsIntervalSeconds), sut.Interval);
    }

    private static async Task<ProcessDiagnosticsSnapshot> WaitForProcessesAsync(ProcessDiagnosticsMonitor sut)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            ProcessDiagnosticsSnapshot snapshot = sut.Snapshot;

            if (snapshot.Processes.Count > 0)
            {
                return snapshot;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Diagnostics snapshot was not sampled in time.");
        return null;
    }
}
