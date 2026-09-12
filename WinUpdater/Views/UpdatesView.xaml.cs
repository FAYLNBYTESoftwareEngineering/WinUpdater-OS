using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinUpdater.Models;
using WinUpdater.ViewModels;

namespace WinUpdater.Views;

public partial class UpdatesView : UserControl
{
    public UpdatesView()
    {
        InitializeComponent();
    }

    private async void ShowUpdatesClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) await vm.CheckForUpdatesAsync();
    }

    private async void UpdateSelectedClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) await vm.UpdateSelectedAsync();
    }

    private void SelectAllClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectAll();
    }

    private void SelectNoneClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectNone();
    }

    // Kontextmenü: das ausgewählte Paket ignorieren
    private void IgnorePackageClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm &&
            UpdateDataGrid.SelectedItem is WingetItem item)
        {
            vm.IgnorePackage(item);
        }
    }

    private async void DiagnoseClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        vm.StatusText = "🔬 Diagnose läuft…";
        string report = await vm.RunDiagnoseAsync();

        // Ergebnis in LogPanel anzeigen
        vm.LogOutputText = report;
        vm.StatusText = "🔬 Diagnose abgeschlossen – bitte Log-Konsole prüfen.";
    }

    // Checkbox reagiert sofort auf ersten Klick (WPF DataGrid Bug)
    private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
        {
            if (!cell.IsFocused) cell.Focus();

            var checkBox = FindVisualChild<CheckBox>(cell);
            if (checkBox != null)
            {
                checkBox.IsChecked = !checkBox.IsChecked;
                e.Handled = true;
            }
        }
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent)
        where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }

        return null;
    }
}