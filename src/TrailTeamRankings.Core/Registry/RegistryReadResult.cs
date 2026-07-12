using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Registry;

/// <summary>
/// Outcome of reading a registry workbook: either a valid set of athletes (with
/// optional non-fatal warnings) or a failure carrying human-readable messages
/// explaining why the file was not accepted.
/// </summary>
public sealed class RegistryReadResult
{
    private RegistryReadResult(
        bool isValid,
        IReadOnlyList<RegisteredAthlete> athletes,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        IsValid = isValid;
        Athletes = athletes;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>True when the file matched the expected format and yielded athletes.</summary>
    public bool IsValid { get; }

    /// <summary>Parsed athletes; empty when <see cref="IsValid"/> is false.</summary>
    public IReadOnlyList<RegisteredAthlete> Athletes { get; }

    /// <summary>Fatal messages explaining why the file was rejected.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Non-fatal messages (e.g. skipped rows, missing medical dates).</summary>
    public IReadOnlyList<string> Warnings { get; }

    public static RegistryReadResult Success(
        IReadOnlyList<RegisteredAthlete> athletes,
        IReadOnlyList<string>? warnings = null) =>
        new(isValid: true, athletes, [], warnings ?? []);

    public static RegistryReadResult Failure(params string[] errors) =>
        new(isValid: false, [], errors, []);
}
