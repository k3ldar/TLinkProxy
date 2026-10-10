using TLinkProxy.Base.Models;
using System.Diagnostics.CodeAnalysis;

[TestClass]
[ExcludeFromCodeCoverage]
public class TokenDailyRecordTests
{
    [TestMethod]
    public void TestConstructor_InitializesPerModelAsEmptyDictionary()
    {
        // Arrange
        var record = new TokenDailyRecord();

        // Act

        // Assert
        Assert.IsNotNull(record.PerModel);
        Assert.IsEmpty(record.PerModel);
    }

    [TestMethod]
    public void TestTokensSaved_CalculatesCorrectly()
    {
        // Arrange
        var record = new TokenDailyRecord
        {
            OriginalTokens = 1000,
            OptimizedTokens = 800
        };

        // Act
        var saved = record.TokensSaved;

        // Assert
        Assert.AreEqual(200, saved);
    }

    [TestMethod]
    public void TestTokensSaved_HandlesNegativeValues()
    {
        // Arrange
        var record = new TokenDailyRecord
        {
            OriginalTokens = 500,
            OptimizedTokens = 600
        };

        // Act
        var saved = record.TokensSaved;

        // Assert
        Assert.AreEqual(-100, saved);
    }

    [TestMethod]
    public void TestPerModel_AddsAndRemovesEntries()
    {
        // Arrange
        var record = new TokenDailyRecord();

        // Act
        record.PerModel.Add("model1", new ModelTokenStats());
        record.PerModel.Remove("model1");

        // Assert
        Assert.IsEmpty(record.PerModel);
    }

    [TestMethod]
    public void TestDate_PropertySetsCorrectly()
    {
        // Arrange
        var record = new TokenDailyRecord
        {
            // Act
            Date = "2026-08-24"
        };

        // Assert
        Assert.AreEqual("2026-08-24", record.Date);
    }

    [TestMethod]
    public void TestRequestCount_PropertySetsCorrectly()
    {
        // Arrange
        var record = new TokenDailyRecord
        {
            // Act
            RequestCount = 42
        };

        // Assert
        Assert.AreEqual(42, record.RequestCount);
    }
}
