using TLinkProxy.Base.Models;
using System.Diagnostics.CodeAnalysis;

[TestClass]
[ExcludeFromCodeCoverage]
public class SessionMetricRecordTests
{
    [TestMethod]
    public void Constructor_Default_ToolInvocationsInitializedEmpty()
    {
        SessionMetricRecord sut = new();

        Assert.IsNotNull(sut.ToolInvocations);
        Assert.IsEmpty(sut.ToolInvocations);
    }

    [TestMethod]
    public void Model_Property_SetsCorrectly()
    {
        SessionMetricRecord sut = new() { Model = "qwen" };

        Assert.AreEqual("qwen", sut.Model);
    }

    [TestMethod]
    public void SessionId_Property_SetsCorrectly()
    {
        SessionMetricRecord sut = new() { SessionId = "abc-123" };

        Assert.AreEqual("abc-123", sut.SessionId);
    }

    [TestMethod]
    public void RecordedAtUtc_Property_SetsCorrectly()
    {
        DateTime expected = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        SessionMetricRecord sut = new() { RecordedAtUtc = expected };

        Assert.AreEqual(expected, sut.RecordedAtUtc);
    }
}
