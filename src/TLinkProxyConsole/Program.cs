using TLinkProxyConsole.Models;
using TLinkProxyConsole.Runner;

namespace TLinkProxyConsole;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        const string RunPrefix = "-r:";

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        List<string> fixturePaths = [];

        foreach (string arg in args)
        {
            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsage();
                return 0;
            }

            if (arg.StartsWith(RunPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string path = arg[RunPrefix.Length..].Trim('"');

                if (String.IsNullOrWhiteSpace(path))
                {
                    WriteError($"Empty path supplied for {RunPrefix} argument.");
                    return 1;
                }

                fixturePaths.Add(path);
                continue;
            }

            WriteError($"Unknown argument: {arg}");
            PrintUsage();
            return 1;
        }

        if (fixturePaths.Count == 0)
        {
            WriteError("No fixture files supplied.  Use -r:\"path/to/fixture.json\".");
            PrintUsage();
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"TLinkProxy Proxy Test Runner");
        Console.WriteLine($"Target: {ConsoleConstants.DefaultServerUrl}");
        Console.WriteLine(new string('─', 60));
        Console.WriteLine();

        List<TestResult> results = [];

        using HttpMcpClient client = new();

        foreach (string path in fixturePaths)
        {
            TestFixture fixture;

            try
            {
                fixture = FixtureLoader.Load(path);
            }
            catch (Exception ex)
            {
                WriteError($"Failed to load '{path}': {ex.Message}");
                results.Add(new TestResult(
                    new TestFixture { Description = path },
                    Passed: false,
                    ActualResponse: null,
                    ErrorMessage: ex.Message));
                continue;
            }

            string actualJson = null;
            string errorMessage = null;
            bool passed = false;

            try
            {
                actualJson = await client.SendAsync(fixture);
                (passed, errorMessage) = ResponseComparer.Compare(fixture, actualJson);
            }
            catch (Exception ex)
            {
                errorMessage = $"Request failed: {ex.Message}";
            }

            if (passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  ✓ PASS");
                Console.ResetColor();
                Console.WriteLine($"  {fixture.Description}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  ✗ FAIL");
                Console.ResetColor();
                Console.WriteLine($"  {fixture.Description}");

                if (!String.IsNullOrWhiteSpace(errorMessage))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (string line in errorMessage.Split('\n'))
                        Console.WriteLine($"       {line}");
                    Console.ResetColor();
                }
            }

            results.Add(new TestResult(fixture, passed, actualJson, errorMessage));
        }

        int passCount = results.Count(r => r.Passed);
        int failCount = results.Count - passCount;

        Console.WriteLine();
        Console.WriteLine(new string('─', 60));

        if (failCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  All {passCount}/{results.Count} test(s) passed.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  {passCount}/{results.Count} passed, {failCount} failed.");
        }

        Console.ResetColor();
        Console.WriteLine();

        return failCount == 0 ? 0 : 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  TLinkProxyConsole.exe -r:\"path/to/fixture.json\" [-r:\"another.json\" ...]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine($"  -r:<file>   Path to a fixture JSON file to run (relative or absolute).");
        Console.WriteLine($"  -h, --help  Show this help message.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  TLinkProxyConsole.exe -r:\"Fixtures/tags.json\"");
        Console.WriteLine("  TLinkProxyConsole.exe -r:\"Fixtures/chat-completions.json\" -r:\"Fixtures/show.json\"");
        Console.WriteLine();
        Console.WriteLine($"Default target: {ConsoleConstants.DefaultServerUrl}");
        Console.WriteLine("  Override per-fixture by setting the \"url\" field in the JSON file.");
        Console.WriteLine();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"ERROR: {message}");
        Console.ResetColor();
    }
}
