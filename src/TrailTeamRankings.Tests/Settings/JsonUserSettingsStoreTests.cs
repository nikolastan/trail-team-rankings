using TrailTeamRankings.Core.Settings;
using TrailTeamRankings.Infrastructure.Settings;

namespace TrailTeamRankings.Tests.Settings;

public class JsonUserSettingsStoreTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"tts-settings-{Guid.NewGuid():N}.json");

    [Fact]
    public void Save_Then_Load_RoundTripsAllFields()
    {
        var path = TempPath();
        try
        {
            var store = new JsonUserSettingsStore(path);
            store.Save(new UserSettings
            {
                RegistryPath = @"C:\reg.xlsx",
                RaceUrl = "https://example/race",
                RaceDate = new DateTime(2026, 9, 1),
                ExportDirectory = @"C:\out",
            });

            var loaded = store.Load();

            Assert.Equal(@"C:\reg.xlsx", loaded.RegistryPath);
            Assert.Equal("https://example/race", loaded.RaceUrl);
            Assert.Equal(new DateTime(2026, 9, 1), loaded.RaceDate);
            Assert.Equal(@"C:\out", loaded.ExportDirectory);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var loaded = new JsonUserSettingsStore(TempPath()).Load();

        Assert.Null(loaded.RegistryPath);
        Assert.Null(loaded.RaceUrl);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileCorrupt()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "{ not valid json ]");

            var loaded = new JsonUserSettingsStore(path).Load();

            Assert.Null(loaded.RaceUrl);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"tts-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        try
        {
            new JsonUserSettingsStore(path).Save(new UserSettings { RaceUrl = "x" });

            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
