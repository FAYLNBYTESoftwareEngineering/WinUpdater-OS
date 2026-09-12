using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinUpdater.Models;

namespace WinUpdater.ViewModels;

public class UpdatePackageViewModel(UpdatePackage model) : INotifyPropertyChanged
{
    private bool isSelected;

    private UpdatePackage Model { get; } = model;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            OnPropertyChanged();
        }
    }

    // Durchgereichte Properties für Bindings
    public string Name => Model.Name;
    public string Id => Model.Id;
    public string CurrentVersion => Model.CurrentVersion;
    public string AvailableVersion => Model.AvailableVersion;
    public string Source => Model.Source;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}