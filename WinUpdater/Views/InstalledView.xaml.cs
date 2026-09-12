using System.Windows;
using WinUpdater.ViewModels;

namespace WinUpdater.Views;

public partial class InstalledView
{
    public InstalledView()
    {
        InitializeComponent();
    }

    private async void LoadInstalledClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) await vm.LoadInstalledProgramsAsync();
    }
}