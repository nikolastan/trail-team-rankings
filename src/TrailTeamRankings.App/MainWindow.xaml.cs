using System.Net.Http;
using System.Windows;
using Microsoft.Win32;
using TrailTeamRankings.Infrastructure.Racing;
using TrailTeamRankings.Infrastructure.Registry;

namespace TrailTeamRankings.App;

/// <summary>
/// Interaction logic for MainWindow.xaml. Composes the view model and hosts the
/// registry file-picker (a dialog is the one piece that belongs in the view).
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
            new RunTraceResultsProvider(HttpClient));
        DataContext = _viewModel;
    }

    private void BrowseRegistry_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select federation registry",
            Filter = "Excel registry (*.xlsx)|*.xlsx|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.LoadRegistryCommand.Execute(dialog.FileName);
        }
    }
}
