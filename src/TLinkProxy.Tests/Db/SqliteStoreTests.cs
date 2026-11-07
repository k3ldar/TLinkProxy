using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using TLinkProxy.Db.Internal;

namespace TLinkProxy.Tests.Db;

[TestClass]
[ExcludeFromCodeCoverage]
public class SqliteStoreTests
{
    private string _databasePath;
    private SqliteDatabase _database;

    [TestInitialize]
    public void Initialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"tlinkproxy-test-{Guid.NewGuid():N}.db");
        _database = new SqliteDatabase(_databasePath);
        _database.EnsureSchema();
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [TestMethod]
    public void Settings_UpdateGetDelete_RoundTripsCaseInsensitively()
    {
        SettingsService sut = new(_database);

        sut.UpdateSetting("Theme", "dark");
        sut.UpdateSetting("THEME", "light");

        Assert.AreEqual("light", sut.GetSetting("theme"));
        Assert.IsTrue(sut.DeleteSetting("Theme"));
        Assert.IsNull(sut.GetSetting("Theme"));
        Assert.IsFalse(sut.DeleteSetting("Theme"));
    }

    [TestMethod]
    public void AiModels_AddUpdateGetDelete_RoundTripsAllFields()
    {
        AiModelStore sut = new(_database);
        AiModel model = new()
        {
            Alias = "qwen",
            Kind = ModelKind.Ollama,
            Endpoint = "http://localhost:11434",
            Model = "qwen2.5",
            Tags = ["gpu", "tool-calling"],
            ComputeType = ModelComputeProfile.Gpu,
            LatencyProfile = ModelLatencyProfile.Fast,
            CostPerMillionTokensInput = 0.27m,
            CostPerMillionTokensOutput = 1.1m,
            MaxContextSize = ModelMaximumContextSize.Size32768,
            ConcurrencyLimit = 2,
            Weight = 70,
            AllowsQueue = true,
            MaxQueueDepth = 8,
            IsEnabled = true,
            ApiKeyEnvVar = "QWEN_KEY",
            AuthScheme = "Bearer",
            AdditionalHeaders = new Dictionary<string, string> { ["anthropic-version"] = "2026-10-21" },
            HealthProbeSeconds = 15,
            GpuLayers = 99,
            Threads = 8,
            BatchSize = 512,
            AutoRestart = true,
            TimeoutSeconds = 120
        };

        sut.AddModel(model);
        model.Weight = 10;
        model.Tags = ["cpu"];
        sut.AddModel(model);

        IAiModel loaded = sut.GetModel("QWEN");

        Assert.HasCount(1, sut.GetAllModels());
        Assert.AreEqual("qwen", loaded.Alias);
        Assert.AreEqual(ModelKind.Ollama, loaded.Kind);
        Assert.AreEqual(0.27m, loaded.CostPerMillionTokensInput);
        Assert.AreEqual(1.1m, loaded.CostPerMillionTokensOutput);
        Assert.AreEqual("Bearer", loaded.AuthScheme);
        Assert.IsNotNull(loaded.AdditionalHeaders);
        Assert.AreEqual("2026-10-21", loaded.AdditionalHeaders["anthropic-version"]);
        Assert.AreEqual(ModelMaximumContextSize.Size32768, loaded.MaxContextSize);
        Assert.AreEqual((byte)10, loaded.Weight);
        Assert.IsTrue(loaded.AllowsQueue);
        Assert.AreEqual(99u, loaded.GpuLayers);
        CollectionAssert.AreEqual(new[] { "cpu" }, loaded.Tags.ToArray());

        Assert.IsTrue(sut.DeleteModel(model));
        Assert.IsNull(sut.GetModel("qwen"));
        Assert.IsFalse(sut.DeleteModel(model));
    }

    [TestMethod]
    public void AiModels_UpdateModel_AddsWhenMissing()
    {
        AiModelStore sut = new(_database);

        Assert.IsTrue(sut.UpdateModel(new AiModel { Alias = "new", Kind = ModelKind.OpenAI, MaxContextSize = ModelMaximumContextSize.Size2048 }));
        Assert.IsNotNull(sut.GetModel("new"));
        Assert.IsFalse(sut.UpdateModel(new AiModel { Alias = " " }));
    }

    [TestMethod]
    public async Task TokenStats_Record_AggregatesTodayMonthAndLifetime()
    {
        TokenStatsStore sut = new(_database);

        await sut.RecordAsync("s1", 400, 200, "qwen");
        await sut.RecordAsync("s1", 40, 20, "qwen");
        await sut.RecordAsync("s2", 8, 4, "llama");

        ITokenDailyRecord today = sut.GetToday();
        ITokenDailyRecord month = sut.GetThisMonth();
        ITokenDailyRecord lifetime = sut.GetLifetime();

        Assert.AreEqual(3, today.RequestCount);
        Assert.AreEqual(100 + 10 + 2, today.OriginalTokens);
        Assert.AreEqual(50 + 5 + 1, today.OptimizedTokens);
        Assert.AreEqual(2, today.PerModel["qwen"].RequestCount);
        Assert.AreEqual(1, today.PerModel["llama"].RequestCount);
        Assert.AreEqual(today.RequestCount, month.RequestCount);
        Assert.AreEqual(today.OriginalTokens, lifetime.OriginalTokens);
        Assert.AreEqual("all", lifetime.Date);
    }

    [TestMethod]
    public void TokenStats_GetToday_CreatesSingleRowPerDay()
    {
        TokenStatsStore sut = new(_database);

        sut.GetToday();
        sut.GetToday();

        using SqliteConnection connection = _database.CreateConnection();
        using SqliteCommand command = connection.CreateCommand("SELECT COUNT(*) FROM DailyTokenCounts;");
        Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [TestMethod]
    public void TokenStats_ReadsRowsInMigratedFormat()
    {
        using (SqliteConnection connection = _database.CreateConnection())
        using (SqliteCommand command = connection.CreateCommand("""
            INSERT INTO DailyTokenCounts (RequestCount, OriginalTokens, OptimizedTokens, PerModel, Created, CreatedTicks, CreatedDay, Updated)
            VALUES (5, 100, 60, '{"qwen":{"RequestCount":5,"OriginalTokens":100,"OptimizedTokens":60}}', '2020-01-02T00:00:00.0000000Z', 0, '2020-01-02', '2020-01-02T00:00:00.0000000Z');
            """))
        {
            command.ExecuteNonQuery();
        }

        ITokenDailyRecord lifetime = new TokenStatsStore(_database).GetLifetime();

        Assert.AreEqual(5, lifetime.RequestCount);
        Assert.AreEqual(60, lifetime.OptimizedTokens);
        Assert.AreEqual(5, lifetime.PerModel["qwen"].RequestCount);
    }

    [TestMethod]
    public void SessionMetrics_RecordAppendsToExistingSessionAndCountsTools()
    {
        SessionMetricStore sut = new(_database);

        sut.Record(NewRecord("s1", "qwen", ("read_file", ToolInvocationOutcome.Allowed)));
        _ = sut.GetToolUsageCounts();
        sut.Record(NewRecord("s1", "qwen", ("read_file", ToolInvocationOutcome.Blocked), ("write_file", ToolInvocationOutcome.Sanitized)));
        sut.Record(NewRecord("s2", "llama", ("read_file", ToolInvocationOutcome.Allowed)));

        List<ISessionMetricRecord> all = sut.GetAll();
        IReadOnlyDictionary<string, int> counts = new SessionMetricStore(_database).GetToolUsageCounts();

        Assert.HasCount(2, all);
        Assert.HasCount(3, all[0].ToolInvocations);
        Assert.AreEqual(ToolInvocationOutcome.Blocked, all[0].ToolInvocations[1].Outcome);
        Assert.AreEqual("llama", all[1].Model);
        Assert.AreEqual(3, counts["read_file"]);
        Assert.AreEqual(1, counts["WRITE_FILE"]);
        Assert.AreEqual(3, sut.GetToolUsageCounts()["read_file"]);
    }

    private static SessionMetricRecord NewRecord(string sessionId, string model, params (string Name, ToolInvocationOutcome Outcome)[] tools)
    {
        SessionMetricRecord record = new() { SessionId = sessionId, Model = model };

        foreach ((string name, ToolInvocationOutcome outcome) in tools)
        {
            record.ToolInvocations.Add(new ToolInvocation { Name = name, Arguments = "{}", Outcome = outcome });
        }

        return record;
    }
}
