using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using TLinkProxy.Abstractions;
using TLinkProxy.Controllers;
using TLinkProxy.Models;

namespace TLinkProxy.Tests.Controllers;

[TestClass]
[ExcludeFromCodeCoverage]
public class HomeControllerDiagnosticsHistoryTests
{
    [TestMethod]
    public void DiagnosticsHistory_MapsSamplesInOrder()
    {
        IProcessDiagnostics diagnostics = Substitute.For<IProcessDiagnostics>();
        diagnostics.Interval.Returns(TimeSpan.FromSeconds(10));
        diagnostics.History.Returns(
        [
            new ProcessDiagnosticsSnapshot { SampledAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), ManagedHeapBytes = 1, GcCommittedBytes = 2, WorkingSetBytes = 3, PrivateMemoryBytes = 4 },
            new ProcessDiagnosticsSnapshot { SampledAtUtc = new DateTime(2026, 1, 1, 0, 0, 10, DateTimeKind.Utc), ManagedHeapBytes = 5, GcCommittedBytes = 6, WorkingSetBytes = 7, PrivateMemoryBytes = 8 }
        ]);

        using HomeController sut = new(
            Substitute.For<IAiBackendRegistry>(),
            Substitute.For<IModelScheduler>(),
            Substitute.For<ILlamaCppProcessManager>(),
            Substitute.For<ITokenStatsStore>(),
            Substitute.For<ISessionMetricStore>(),
            diagnostics);

        JsonResult result = (JsonResult)sut.DiagnosticsHistory();
        DashboardMemoryHistoryDto dto = (DashboardMemoryHistoryDto)result.Value;

        Assert.AreEqual(10, dto.IntervalSeconds);
        Assert.HasCount(2, dto.Samples);
        Assert.AreEqual(1, dto.Samples[0].ManagedHeapBytes);
        Assert.AreEqual(8, dto.Samples[1].PrivateMemoryBytes);
        Assert.IsLessThan(dto.Samples[1].SampledAtUtc, dto.Samples[0].SampledAtUtc);
    }

    [TestMethod]
    public void DiagnosticsHistory_NoSamples_ReturnsEmptyList()
    {
        IProcessDiagnostics diagnostics = Substitute.For<IProcessDiagnostics>();
        diagnostics.Interval.Returns(TimeSpan.FromSeconds(10));
        diagnostics.History.Returns([]);

        using HomeController sut = new(
            Substitute.For<IAiBackendRegistry>(),
            Substitute.For<IModelScheduler>(),
            Substitute.For<ILlamaCppProcessManager>(),
            Substitute.For<ITokenStatsStore>(),
            Substitute.For<ISessionMetricStore>(),
            diagnostics);

        DashboardMemoryHistoryDto dto = (DashboardMemoryHistoryDto)((JsonResult)sut.DiagnosticsHistory()).Value;

        Assert.IsEmpty(dto.Samples);
    }
}
