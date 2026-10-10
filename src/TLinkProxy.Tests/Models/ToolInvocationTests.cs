using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using System.Diagnostics.CodeAnalysis;

[TestClass]
[ExcludeFromCodeCoverage]
public class ToolInvocationTests
{
    [TestMethod]
    public void Constructor_Default_PropertiesEmpty()
    {
        ToolInvocation sut = new();

        Assert.AreEqual(String.Empty, sut.Name);
        Assert.AreEqual(String.Empty, sut.Arguments);
    }

    [TestMethod]
    public void Name_Property_SetsCorrectly()
    {
        ToolInvocation sut = new() { Name = "read_file" };

        Assert.AreEqual("read_file", sut.Name);
    }

    [TestMethod]
    public void Arguments_Property_SetsCorrectly()
    {
        ToolInvocation sut = new() { Arguments = "{\"path\":\"/repo\"}" };

        Assert.AreEqual("{\"path\":\"/repo\"}", sut.Arguments);
    }

    [TestMethod]
    public void Outcome_Property_DefaultsToAllowed()
    {
        ToolInvocation sut = new();

        Assert.AreEqual(ToolInvocationOutcome.Allowed, sut.Outcome);
    }

    [TestMethod]
    public void Outcome_Property_SetsCorrectly()
    {
        ToolInvocation sut = new() { Outcome = ToolInvocationOutcome.Blocked };

        Assert.AreEqual(ToolInvocationOutcome.Blocked, sut.Outcome);
    }
}
