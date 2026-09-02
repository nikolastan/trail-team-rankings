using System.Text.Json;
using TrailTeamRankings.Core.Settings;

namespace TrailTeamRankings.Infrastructure.Settings;

/// <summary>
/// Stores <see cref="UserSettings"/> as JSON. By default it lives under
/// %AppData%\TrailTeamRankings\settings.json. Load and save are best-effort:
/// a missing, unreadable, or corrupt file yields empty defaults rather than an
/// exception, so settings can never break startup or shutdown.
/// </summary>
public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public JsonUserSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrailTeamRankings",
            "settings.json"))
    {
    }

    public JsonUserSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new UserSettings();
            }

            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_path)) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // Persistence is a convenience; failing to save must not disrupt the app.
        }
    }
}
