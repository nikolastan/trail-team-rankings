using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Win32;
using TrailTeamRankings.Infrastructure.Export;
using TrailTeamRankings.Infrastructure.Racing;
using TrailTeamRankings.Infrastructure.Registry;
using TrailTeamRankings.Infrastructure.Settings;

namespace TrailTeamRankings.App;

/// <summary>
/// Interaction logic for MainWindow.xaml. Composes the view model, hosts the
/// file dialogs (the one piece that belongs in the view), and persists settings
/// across sessions.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly HttpClient HttpClient = new();

    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(
            new RegistryExcelReader(),
            new RunTraceResultsProvider(HttpClient),
            new ExcelResultsExporter(),
            new PdfResultsExporter(),
            new JsonUserSettingsStore());
        DataContext = _viewModel;

        Loaded += (_, _) => _viewModel.TryLoadSavedRegistry();
        Closing += (_, _) => _viewModel.SaveSettings();
    }

    private void BrowseRegistry_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select federation registry",
            Filter = "Excel registry (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            InitialDirectory = ExistingDirectory(Path.GetDirectoryName(_viewModel.RegistryPath)),
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.LoadRegistryCommand.Execute(dialog.FileName);
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export results to Excel",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = "trail-team-rankings.xlsx",
            DefaultExt = "xlsx",
            InitialDirectory = ExistingDirectory(_viewModel.ExportDirectory),
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportExcelCommand.Execute(dialog.FileName);
        }
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export results to PDF",
            Filter = "PDF document (*.pdf)|*.pdf",
            FileName = "trail-team-rankings.pdf",
            DefaultExt = "pdf",
            InitialDirectory = ExistingDirectory(_viewModel.ExportDirectory),
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportPdfCommand.Execute(dialog.FileName);
        }
    }

    private static string ExistingDirectory(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : string.Empty;
}
