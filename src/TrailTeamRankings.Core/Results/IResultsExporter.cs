namespace TrailTeamRankings.Core.Results;

/// <summary>
/// Writes a <see cref="RaceResults"/> to a file format (Excel, PDF, …).
/// Implementations live in Infrastructure; the UI depends only on this
/// abstraction so an export menu can offer several formats uniformly.
/// </summary>
public interface IResultsExporter
{
    /// <summary>File extension without the leading dot, e.g. "xlsx".</summary>
    string FileExtension { get; }

    /// <summary>Writes the results to the destination stream. The caller owns the stream.</summary>
    void Export(RaceResults results, Stream destination);
}
