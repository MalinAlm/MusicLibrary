using System.Collections.ObjectModel;
using MusicLibrary;

namespace MusicLibrary.ViewModels;

public class AlbumNodeViewModel
{
    public int AlbumId { get; init; }
    public string Title { get; init; } = "";
    public ObservableCollection<Track> Tracks { get; } = new();
}
