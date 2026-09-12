using System.Windows;
using WinUpdater.ViewModels;

namespace WinUpdater.Views;

public partial class IgnoreView
{
    public IgnoreView()
    {
        InitializeComponent();
        // Liste beim Laden der View aktualisieren
        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RefreshIgnoreList();
        };
    }

    private void RemoveSelectedClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var selected = IgnoreListBox.SelectedItems.Cast<string>().ToList();
        foreach (var id in selected)
            vm.UnignorePackage(id);
    }

    private void RemoveAllClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (MessageBox.Show("Alle ignorierten Pakete entfernen?", "Bestätigung",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var all = vm.IgnoredPackages.ToList();
            foreach (var id in all)
                vm.UnignorePackage(id);
        }
    }
}