namespace TrailTeamRankings.Core.Settings;

/// <summary>
/// Persisted UI preferences so the app reopens where the user left off
/// (see plan §10). Never contains registry contents — only the file path.
/// </summary>
public sealed class UserSettings
{
    public string? RegistryPath { get; set; }

    public string? RaceUrl { get; set; }

    public DateTime? RaceDate { get; set; }

    public string? ExportDirectory { get; set; }
}
