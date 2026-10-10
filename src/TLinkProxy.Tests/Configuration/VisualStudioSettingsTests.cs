using TLinkProxy.Base.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace TLinkProxy.Tests.Configuration
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class VisualStudioSettingsTests
    {
        [TestMethod]
        public void FromJson_Null_ReturnsDefaults()
        {
            VisualStudioSettings sut = VisualStudioSettings.FromJson(null);

            Assert.IsTrue(sut.Enabled);
            Assert.IsTrue(sut.TrimLineEnds);
            Assert.IsTrue(sut.CollapseBlankLines);
            Assert.IsNotNull(sut.RemoveBlocks);
            Assert.IsEmpty(sut.RemoveBlocks);
            Assert.IsNotNull(sut.RemoveTextPatterns);
            Assert.IsEmpty(sut.RemoveTextPatterns);
            Assert.IsNotNull(sut.RemoveRegexPatterns);
            Assert.IsEmpty(sut.RemoveRegexPatterns);
            Assert.IsNotNull(sut.CopilotInstructions);
            Assert.IsEmpty(sut.CopilotInstructions);
        }

        [TestMethod]
        public void FromJson_Whitespace_ReturnsDefaults()
        {
            VisualStudioSettings sut = VisualStudioSettings.FromJson("   ");

            Assert.IsTrue(sut.Enabled);
        }

        [TestMethod]
        public void FromJson_InvalidJson_ReturnsDefaults()
        {
            VisualStudioSettings sut = VisualStudioSettings.FromJson("{ not valid json");

            Assert.IsTrue(sut.Enabled);
        }

        [TestMethod]
        public void FromJson_PartialJson_FillsMissingFieldsWithDefaults()
        {
            const string json = "{\"enabled\":false}";

            VisualStudioSettings sut = VisualStudioSettings.FromJson(json);

            Assert.IsFalse(sut.Enabled);
            Assert.IsTrue(sut.TrimLineEnds);
            Assert.IsTrue(sut.CollapseBlankLines);
            Assert.IsEmpty(sut.RemoveBlocks);
            Assert.IsEmpty(sut.CopilotInstructions);
        }

        [TestMethod]
        public void ToJson_ThenFromJson_RoundTripsAllFields()
        {
            VisualStudioSettings sut = new()
            {
                Enabled = false,
                TrimLineEnds = false,
                CollapseBlankLines = false,
                RemoveBlocks = ["version_information", "model_information"],
                RemoveTextPatterns = ["Version number:"],
                RemoveRegexPatterns = ["<tool>.*?</tool>"],
                CopilotInstructions = ["Prefer explicit types.", "Use UTC in tests."]
            };

            VisualStudioSettings roundTripped = VisualStudioSettings.FromJson(sut.ToJson());

            Assert.IsFalse(roundTripped.Enabled);
            Assert.IsFalse(roundTripped.TrimLineEnds);
            Assert.IsFalse(roundTripped.CollapseBlankLines);
            CollectionAssert.AreEqual(sut.RemoveBlocks, roundTripped.RemoveBlocks);
            CollectionAssert.AreEqual(sut.RemoveTextPatterns, roundTripped.RemoveTextPatterns);
            CollectionAssert.AreEqual(sut.RemoveRegexPatterns, roundTripped.RemoveRegexPatterns);
            CollectionAssert.AreEqual(sut.CopilotInstructions, roundTripped.CopilotInstructions);
        }

        [TestMethod]
        public void SplitLines_NullOrWhitespace_ReturnsEmptyList()
        {
            Assert.IsEmpty(VisualStudioSettings.SplitLines(null));
            Assert.IsEmpty(VisualStudioSettings.SplitLines("   "));
        }

        [TestMethod]
        public void SplitLines_MixedLineEndings_ReturnsTrimmedUniqueEntries()
        {
            const string input = "version_information\r\n  model_information  \nversion_information\n\n";

            List<string> result = VisualStudioSettings.SplitLines(input);

            Assert.HasCount(2, result);
            Assert.AreEqual("version_information", result[0]);
            Assert.AreEqual("model_information", result[1]);
        }

        [TestMethod]
        public void JoinLines_Null_ReturnsEmptyString()
        {
            string result = VisualStudioSettings.JoinLines(null);

            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        public void JoinLines_IgnoresWhitespaceEntries()
        {
            string result = VisualStudioSettings.JoinLines(["one", "   ", "two"]);

            Assert.DoesNotContain("   ", result);
        }

        [TestMethod]
        public void JoinLines_ThenSplitLines_RoundTrips()
        {
            List<string> values = ["alpha", "beta", "gamma"];

            string joined = VisualStudioSettings.JoinLines(values);
            List<string> result = VisualStudioSettings.SplitLines(joined);

            CollectionAssert.AreEqual(values, result);
        }
    }
}
