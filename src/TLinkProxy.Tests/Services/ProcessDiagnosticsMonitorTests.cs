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

    [TestMethod]
    public async Task History_TrimsToConfiguredCapacity_OldestFirst()
    {
        ISettingsService settings = Substitute.For<ISettingsService>();
        settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns("1");
        // A value below the minimum is clamped up to the minimum capacity.
        settings.GetSetting(Constants.DiagnosticsHistorySamplesKey).Returns("1");
        ILlamaCppProcessManager processManager = Substitute.For<ILlamaCppProcessManager>();
        processManager.GetProcessMemory().Returns([new ProcessMemorySample { Alias = "a" }]);

        using ProcessDiagnosticsMonitor sut = new(settings, processManager);

        await sut.StartAsync(CancellationToken.None);

        IReadOnlyList<ProcessDiagnosticsSnapshot> history = await WaitForHistoryAsync(sut, Constants.MinDiagnosticsHistorySamples, TimeSpan.FromSeconds(2));
        await Task.Delay(1500);
        history = sut.History;
        await sut.StopAsync(CancellationToken.None);

        Assert.HasCount(Constants.MinDiagnosticsHistorySamples, history);
        Assert.IsLessThan(history[1].SampledAtUtc, history[0].SampledAtUtc);
        Assert.IsGreaterThan(0, history[0].WorkingSetBytes);
    }

    [TestMethod]
    public async Task History_NoSettingConfigured_StartsEmptyThenRecordsSamples()
    {
        ISettingsService settings = Substitute.For<ISettingsService>();
        ILlamaCppProcessManager processManager = Substitute.For<ILlamaCppProcessManager>();
        processManager.GetProcessMemory().Returns([new ProcessMemorySample { Alias = "a" }]);

        using ProcessDiagnosticsMonitor sut = new(settings, processManager);

        Assert.IsEmpty(sut.History);

        await sut.StartAsync(CancellationToken.None);
        IReadOnlyList<ProcessDiagnosticsSnapshot> history = await WaitForHistoryAsync(sut, 1, TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        Assert.IsGreaterThanOrEqualTo(1, history.Count);
    }

    private static async Task<IReadOnlyList<ProcessDiagnosticsSnapshot>> WaitForHistoryAsync(ProcessDiagnosticsMonitor sut, int count, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            IReadOnlyList<ProcessDiagnosticsSnapshot> history = sut.History;

            if (history.Count >= count)
            {
                return history;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Diagnostics history was not populated in time.");
        return null;
    }

    private static async Task<ProcessDiagnosticsSnapshot> WaitForProcessesAsync(ProcessDiagnosticsMonitor sut)    {
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
