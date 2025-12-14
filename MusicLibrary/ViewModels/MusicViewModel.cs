using MusicLibrary.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicLibrary.ViewModels
{
    public class MusicViewModel
    {

        private readonly MusicService _service = new();

        public ObservableCollection<Playlist> Playlists { get; set; } = new();
        public ObservableCollection<Track> Tracks { get; set; } = new();

        public async Task LoadDataAsync()
        {
            var playlists = await _service.GetPlaylistsAsync();
            Playlists.Clear();
            foreach (var p in playlists)
                Playlists.Add(p);

            var tracks = await _service.GetTracksAsync();
            Tracks.Clear();
            foreach (var t in tracks)
                Tracks.Add(t);
        }
    }
}
