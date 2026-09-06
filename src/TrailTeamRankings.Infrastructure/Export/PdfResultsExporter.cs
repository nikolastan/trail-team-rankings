using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TrailTeamRankings.Core.Ranking;
using TrailTeamRankings.Core.Results;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Infrastructure.Export;

/// <summary>
/// Exports <see cref="RaceResults"/> to a printable PDF report using QuestPDF:
/// per division, the full team standings plus a brief top-N individual summary
/// (men and women), per the plan's "teams + brief individual summary".
/// </summary>
public sealed class PdfResultsExporter : IResultsExporter
{
    private const int IndividualsTop = 10;

    static PdfResultsExporter()
    {
        // Free Community license (individuals / small orgs / education).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string FileExtension => "pdf";

    public void Export(RaceResults results, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(destination);

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text(results.RaceTitle ?? "Trail Team Rankings").FontSize(16).Bold();
                    header.Item().Text(
                            $"Race date: {results.RaceDate:dd.MM.yyyy}   •   Generated {results.GeneratedAt:dd.MM.yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(16);

                    var any = false;
                    if (results.Seniori.HasRunners)
                    {
                        ComposeDivision(column.Item(), "SENIORI", results.Seniori);
                        any = true;
                    }

                    if (results.Juniori.HasRunners)
                    {
                        ComposeDivision(column.Item(), "JUNIORI", results.Juniori);
                        any = true;
                    }

                    if (!any)
                    {
                        column.Item().Text("No results to display.").Italic();
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(destination);
    }

    private static void ComposeDivision(IContainer container, string title, DivisionResults division)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(title).FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

            column.Item().Text("Team standings (best 2 men + 1 woman by points)").SemiBold();
            TeamTable(column.Item(), division.TeamStandings);

            column.Item().PaddingTop(4).Text($"Individuals (top {IndividualsTop})").SemiBold();
            column.Item().Row(row =>
            {
                IndividualsTable(row.RelativeItem(), "Muškarci", division.MaleIndividuals);
                row.ConstantItem(14);
                IndividualsTable(row.RelativeItem(), "Žene", division.FemaleIndividuals);
            });
        });
    }

    private static void TeamTable(IContainer container, IReadOnlyList<TeamStanding> teams)
    {
        if (teams.Count == 0)
        {
            container.Text("No teams.").Italic().FontColor(Colors.Grey.Medium);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn();
                columns.ConstantColumn(40);
                columns.ConstantColumn(40);
                columns.ConstantColumn(44);
                columns.ConstantColumn(48);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "Klub");
                HeaderCell(header.Cell(), "muš 1");
                HeaderCell(header.Cell(), "muš 2");
                HeaderCell(header.Cell(), "žena 1");
                HeaderCell(header.Cell(), "ukupno");
            });

            foreach (var team in teams)
            {
                DataCell(table.Cell(), team.Rank.ToString());
                DataCell(table.Cell(), team.Club);
                DataCell(table.Cell(), Slot(team.CountingMales, 0));
                DataCell(table.Cell(), Slot(team.CountingMales, 1));
                DataCell(table.Cell(), team.CountingFemale?.Points.ToString() ?? "");
                DataCell(table.Cell(), team.TotalPoints.ToString());
            }
        });
    }

    private static void IndividualsTable(
        IContainer container, string title, IReadOnlyList<IndividualResult> individuals)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(9).SemiBold();

            if (individuals.Count == 0)
            {
                column.Item().Text("—").FontColor(Colors.Grey.Medium);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(30);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "#");
                    HeaderCell(header.Cell(), "Ime i prezime");
                    HeaderCell(header.Cell(), "Klub");
                    HeaderCell(header.Cell(), "bod.");
                });

                foreach (var individual in individuals.Take(IndividualsTop))
                {
                    DataCell(table.Cell(), individual.Rank.ToString());
                    DataCell(table.Cell(), individual.Name);
                    DataCell(table.Cell(), individual.Club);
                    DataCell(table.Cell(), individual.Points.ToString());
                }
            });
        });
    }

    private static void HeaderCell(IContainer cell, string text) =>
        cell.Background(Colors.Grey.Lighten3).Padding(3).Text(text).SemiBold().FontSize(8);

    private static void DataCell(IContainer cell, string text) =>
        cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text).FontSize(8);

    private static string Slot(IReadOnlyList<ScoredRunner> males, int index) =>
        index < males.Count ? males[index].Points.ToString() : "";
}
