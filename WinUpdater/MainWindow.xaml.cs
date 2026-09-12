using System.Windows;
using WinUpdater.ViewModels;
using Wpf.Ui.Controls;

namespace WinUpdater;

public partial class MainWindow
{
    private readonly MainViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new MainViewModel();
        DataContext = viewModel;
    }

    private void MenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationViewItem item)
        {
            viewModel.CurrentView = item.Tag?.ToString() switch
            {
                "Home" => viewModel.HomeViewInstance,
                "Updates" => viewModel.UpdatesViewInstance,
                "Installed" => viewModel.InstalledViewInstance,
                "History" => viewModel.HistoryViewInstance,
                "Logs" => viewModel.LogPanelInstance,
                "Ignore" => viewModel.IgnoreViewInstance,
                _ => viewModel.CurrentView
            };
        }
    }
}