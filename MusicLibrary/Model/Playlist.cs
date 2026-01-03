using System.ComponentModel;

namespace MusicLibrary;

public partial class Playlist : INotifyPropertyChanged
{
    private string? _name;

    public int PlaylistId { get; set; }

    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
