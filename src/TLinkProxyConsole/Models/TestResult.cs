namespace TLinkProxyConsole.Models
{
    /// <summary>
    /// The outcome of running a single <see cref="TestFixture"/>.
    /// </summary>
    internal sealed record TestResult(
        TestFixture Fixture,
        bool Passed,
        string ActualResponse,
        string ErrorMessage);
}
