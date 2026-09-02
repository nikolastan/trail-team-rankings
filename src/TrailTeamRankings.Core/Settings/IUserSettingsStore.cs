namespace TrailTeamRankings.Core.Settings;

/// <summary>
/// Loads and saves <see cref="UserSettings"/>. Implementations must be
/// best-effort: a missing or corrupt store must never throw on load.
/// </summary>
public interface IUserSettingsStore
{
    UserSettings Load();

    void Save(UserSettings settings);
}
