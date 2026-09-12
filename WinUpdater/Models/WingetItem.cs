using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUpdater.Models;

public class WingetItem : INotifyPropertyChanged
{
    private bool isSelected = true;
    private readonly string name = string.Empty;
    private readonly string id = string.Empty;
    private string version = string.Empty;
    private string availableVersion = string.Empty;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => name;
        init
        {
            name = value;
            OnPropertyChanged();
        }
    }

    public string Id
    {
        get => id;
        init
        {
            id = value;
            OnPropertyChanged();
        }
    }

    public string Version
    {
        get => version;
        set
        {
            version = value;
            OnPropertyChanged();
        }
    }

    public string AvailableVersion
    {
        get => availableVersion;
        set
        {
            availableVersion = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}