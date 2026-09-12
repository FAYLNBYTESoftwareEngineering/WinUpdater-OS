using System.IO;
using System.Windows;
using WinUpdater.ViewModels;

namespace WinUpdater.Views;

public partial class HistoryView
{
    public HistoryView()
    {
        InitializeComponent();
        Loaded += (s, e) => ReadLogFile();
    }

    private void LoadHistoryClick(object sender, RoutedEventArgs e)
    {
        ReadLogFile();
    }

    private void ClearHistoryClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && File.Exists(vm.LogFilePath))
        {
            if (MessageBox.Show("Möchtest du den gesamten Update-Verlauf löschen?", "Verlauf löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    File.WriteAllText(vm.LogFilePath, string.Empty);
                    HistoryTextBox.Text = "Der Verlauf wurde gelöscht.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen: {ex.Message}");
                }
            }
        }
    }

    private void ReadLogFile()
    {
        if (DataContext is MainViewModel vm && File.Exists(vm.LogFilePath))
        {
            try
            {
                string content = File.ReadAllText(vm.LogFilePath);
                HistoryTextBox.Text = string.IsNullOrWhiteSpace(content)
                    ? "Noch keine Einträge im Verlauf vorhanden."
                    : content;
            }
            catch (Exception ex)
            {
                HistoryTextBox.Text = $"Fehler beim Laden des Verlaufs: {ex.Message}";
            }
        }
        else
        {
            HistoryTextBox.Text = "Noch kein Verlauf aufgezeichnet.";
        }
    }
}