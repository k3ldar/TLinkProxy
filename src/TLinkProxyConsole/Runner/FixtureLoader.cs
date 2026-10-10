using TLinkProxyConsole.Models;
using System.Text.Json;

namespace TLinkProxyConsole.Runner
{
    internal static class FixtureLoader
    {
        private const string FixturesDirectoryName = "Fixtures";

        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// Loads and deserializes a <see cref="TestFixture"/> from <paramref name="path"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="InvalidOperationException">The file cannot be parsed or is missing required fields.</exception>
        internal static TestFixture Load(string path)
        {
            string fullPath = ResolvePath(path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Fixture file not found: {fullPath}");

            string json;

            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Cannot read fixture file '{fullPath}': {ex.Message}", ex);
            }

            TestFixture fixture;

            try
            {
                fixture = JsonSerializer.Deserialize<TestFixture>(json, _options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Fixture file '{fullPath}' contains invalid JSON: {ex.Message}", ex);
            }

            if (fixture is null)
                throw new InvalidOperationException($"Fixture file '{fullPath}' deserialised to null.");

            if (String.IsNullOrWhiteSpace(fixture.Description))
                throw new InvalidOperationException(
                    $"Fixture file '{fullPath}' is missing the required 'description' field.");

            if (String.IsNullOrWhiteSpace(fixture.Url) && String.IsNullOrWhiteSpace(fixture.Path))
                throw new InvalidOperationException(
                    $"Fixture file '{fullPath}' must supply either a 'path' or a full 'url'.");

            bool relaxed = fixture.Conversation || fixture.NonEmptyArray;

            if (!relaxed && fixture.ExpectedResponse.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                throw new InvalidOperationException(
                    $"Fixture file '{fullPath}' is missing the required 'expectedResponse' field.");

            return fixture;
        }

        /// <summary>
        /// Resolves a fixture path relative to the executing assembly's <c>Fixtures</c>
        /// subfolder, unless <paramref name="path"/> is already rooted.
        /// </summary>
        private static string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            string baseDir = AppContext.BaseDirectory;

            return Path.GetFullPath(Path.Combine(baseDir, FixturesDirectoryName, path));
        }
    }
}
