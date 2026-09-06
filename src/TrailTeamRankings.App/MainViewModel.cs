using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Racing;
using TrailTeamRankings.Core.Registry;
using TrailTeamRankings.Core.Results;
using TrailTeamRankings.Core.Settings;

namespace TrailTeamRankings.App;

/// <summary>
/// Drives the main window: the setup gate (registry, URL, date), the refresh /
/// live-poll commands, and the resolved <see cref="RaceResults"/> projected into
/// the collections the tabs bind to.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IRegistryReader _registryReader;
    private readonly IRaceResultsProvider _resultsProvider;
    private readonly IResultsExporter _excelExporter;
    private readonly IResultsExporter _pdfExporter;
    private readonly IUserSettingsStore _settingsStore;
    private readonly DispatcherTimer _liveTimer;

    private IReadOnlyList<RegisteredAthlete> _athletes = [];
    private string? _exportDirectory;

    public MainViewModel(
        IRegistryReader registryReader,
        IRaceResultsProvider resultsProvider,
        IResultsExporter excelExporter,
        IResultsExporter pdfExporter,
        IUserSettingsStore settingsStore)
    {
        _registryReader = registryReader;
        _resultsProvider = resultsProvider;
        _excelExporter = excelExporter;
        _pdfExporter = pdfExporter;
        _settingsStore = settingsStore;

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        _liveTimer.Tick += async (_, _) =>
        {
            if (RefreshCommand.CanExecute(null))
            {
                await RefreshCommand.ExecuteAsync(null);
            }
        };

        ApplySavedSettings();
    }

    /// <summary>Directory of the last export, used to pre-select the save dialog.</summary>
    public string? ExportDirectory => _exportDirectory;

    private void ApplySavedSettings()
    {
        var settings = _settingsStore.Load();

        if (!string.IsNullOrWhiteSpace(settings.RaceUrl))
        {
            RaceUrl = settings.RaceUrl;
        }

        if (settings.RaceDate is { } savedDate)
        {
            RaceDate = savedDate;
        }

        RegistryPath = settings.RegistryPath;
        _exportDirectory = settings.ExportDirectory;
    }

    /// <summary>Persists the current inputs. Called on shutdown and after key actions.</summary>
    public void SaveSettings() => _settingsStore.Save(new UserSettings
    {
        RegistryPath = RegistryPath,
        RaceUrl = RaceUrl,
        RaceDate = RaceDate,
        ExportDirectory = _exportDirectory,
    });

    /// <summary>Loads the previously used registry automatically if it still exists.</summary>
    public void TryLoadSavedRegistry()
    {
        if (!string.IsNullOrWhiteSpace(RegistryPath) && File.Exists(RegistryPath))
        {
            LoadRegistryCommand.Execute(RegistryPath);
        }
    }

    // ---- Setup gate ----
    [ObservableProperty]
    private string? registryPath;

    [ObservableProperty]
    private int registryAthleteCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool registryLoaded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private string raceUrl = "https://runtrace.net/avala2026?category_id=&race_id=1123&selected_lang=sr-Latn";

    [ObservableProperty]
    private DateTime raceDate = DateTime.Today;

    // ---- Status ----
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private DateTimeOffset? lastUpdated;

    [ObservableProperty]
    private bool liveMode;

    // ---- Results (projected for the tabs) ----
    [ObservableProperty]
    private RaceResults? results;

    [ObservableProperty]
    private bool hasResults;

    [ObservableProperty]
    private IReadOnlyList<TeamRow> senioriTeams = [];

    [ObservableProperty]
    private IReadOnlyList<TeamRow> junioriTeams = [];

    [ObservableProperty]
    private IReadOnlyList<RaceRunner> allRunners = [];

    [ObservableProperty]
    private IReadOnlyList<ExcludedRunner> excludedRunners = [];

    [ObservableProperty]
    private string excludedHeader = "Excluded";

    [ObservableProperty]
    private string senioriTeamsHeader = "Seniori teams";

    [ObservableProperty]
    private string junioriTeamsHeader = "Juniori teams";

    [ObservableProperty]
    private bool senioriTeamsEmpty;

    [ObservableProperty]
    private bool junioriTeamsEmpty;

    [ObservableProperty]
    private Division individualsDivision = Division.Seniori;

    [ObservableProperty]
    private IReadOnlyList<IndividualResult> menIndividuals = [];

    [ObservableProperty]
    private IReadOnlyList<IndividualResult> womenIndividuals = [];

    public IReadOnlyList<Division> Divisions { get; } = [Division.Seniori, Division.Juniori];

    // ---- Commands ----
    [RelayCommand]
    private async Task LoadRegistry(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        StatusMessage = "Loading registry…";
        try
        {
            var result = await Task.Run(() =>
            {
                using var stream = File.OpenRead(path);
                return _registryReader.Read(stream);
            });

            if (!result.IsValid)
            {
                RegistryLoaded = false;
                ErrorMessage = "Registry not loaded: " + string.Join(" ", result.Errors);
                return;
            }

            _athletes = result.Athletes;
            RegistryPath = path;
            RegistryAthleteCount = result.Athletes.Count;
            RegistryLoaded = true;
            StatusMessage = $"Registry loaded: {result.Athletes.Count} athletes.";
            SaveSettings();
        }
        catch (Exception ex)
        {
            RegistryLoaded = false;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task Refresh()
    {
        ErrorMessage = null;
        IsBusy = true;
        StatusMessage = "Fetching results…";
        try
        {
            var scrape = await _resultsProvider.GetResultsAsync(RaceUrl);
            if (!scrape.IsValid)
            {
                ErrorMessage = "Scrape failed: " + string.Join(" ", scrape.Errors);
                return;
            }

            var raceDateOnly = DateOnly.FromDateTime(RaceDate);
            var built = await Task.Run(() =>
                RaceResultsBuilder.Build(_athletes, scrape.Runners, raceDateOnly, scrape.RaceTitle));

            Results = built;
            LastUpdated = DateTimeOffset.Now;
            StatusMessage = $"{built.AllRunners.Count} runners" +
                            (scrape.RaceTitle is { Length: > 0 } title ? $" • {title}" : "");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRefresh() => RegistryLoaded && !IsBusy && !string.IsNullOrWhiteSpace(RaceUrl);

    [RelayCommand]
    private Task ExportExcel(string? path) => ExportWith(_excelExporter, path);

    [RelayCommand]
    private Task ExportPdf(string? path) => ExportWith(_pdfExporter, path);

    private async Task ExportWith(IResultsExporter exporter, string? path)
    {
        if (Results is null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var results = Results;
        ErrorMessage = null;
        IsBusy = true;
        StatusMessage = "Exporting…";
        try
        {
            await Task.Run(() =>
            {
                using var stream = File.Create(path);
                exporter.Export(results, stream);
            });
            _exportDirectory = Path.GetDirectoryName(path);
            StatusMessage = $"Exported to {path}";
            SaveSettings();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Export failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---- Projection of Results into bound collections ----
    partial void OnResultsChanged(RaceResults? value)
    {
        HasResults = value is not null;

        if (value is null)
        {
            SenioriTeams = [];
            JunioriTeams = [];
            AllRunners = [];
            ExcludedRunners = [];
            MenIndividuals = [];
            WomenIndividuals = [];
            ExcludedHeader = "Excluded";
            SenioriTeamsHeader = "Seniori teams";
            JunioriTeamsHeader = "Juniori teams";
            SenioriTeamsEmpty = false;
            JunioriTeamsEmpty = false;
            return;
        }

        SenioriTeams = value.Seniori.TeamStandings.Select(TeamRow.From).ToList();
        JunioriTeams = value.Juniori.TeamStandings.Select(TeamRow.From).ToList();
        AllRunners = value.AllRunners;

        var excluded = value.Seniori.ExcludedRunners
            .Concat(value.Juniori.ExcludedRunners)
            .Concat(value.UnclassifiedExcluded)
            .ToList();
        ExcludedRunners = excluded;
        ExcludedHeader = $"Excluded ({excluded.Count})";
        SenioriTeamsHeader = $"Seniori teams ({SenioriTeams.Count})";
        JunioriTeamsHeader = $"Juniori teams ({JunioriTeams.Count})";
        SenioriTeamsEmpty = SenioriTeams.Count == 0;
        JunioriTeamsEmpty = JunioriTeams.Count == 0;

        UpdateIndividuals();
    }

    partial void OnIndividualsDivisionChanged(Division value) => UpdateIndividuals();

    private void UpdateIndividuals()
    {
        var division = Results?.For(IndividualsDivision);
        MenIndividuals = division?.MaleIndividuals ?? [];
        WomenIndividuals = division?.FemaleIndividuals ?? [];
    }

    partial void OnLiveModeChanged(bool value)
    {
        if (value)
        {
            _liveTimer.Start();
            if (RefreshCommand.CanExecute(null))
            {
                RefreshCommand.Execute(null);
            }
        }
        else
        {
            _liveTimer.Stop();
        }
    }
}
