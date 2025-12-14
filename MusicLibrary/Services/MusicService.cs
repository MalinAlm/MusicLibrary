using Microsoft.EntityFrameworkCore;
using MusicLibrary;

namespace MusicLibrary.Services
{
    public class MusicService
    {
        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            using var db = new MusicContext();
            return await db.Playlists
                           .OrderBy(p => p.Name)
                           .ToListAsync();
        }

       
        public async Task<List<Track>> GetTracksAsync()
        {
            using var db = new MusicContext();
            return await db.Tracks
                           .Include(t => t.Album)
                           .ThenInclude(a => a.Artist)
                           .ToListAsync();
        }

        
        public async Task<List<Track>> GetTracksForPlaylistAsync(int playlistId)
        {
            using var db = new MusicContext();

            return await db.PlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .Include(pt => pt.Track)
                .ThenInclude(t => t.Album)
                .Select(pt => pt.Track)
                .ToListAsync();
        }

       
        public async Task<List<Album>> GetAlbumsAsync()
        {
            using var db = new MusicContext();
            return await db.Albums
                           .Include(a => a.Artist)
                           .Include(a => a.Tracks)
                           .ToListAsync();
        }

       
        public async Task<List<Artist>> GetArtistsAsync()
        {
            using var db = new MusicContext();
            return await db.Artists
                           .Include(a => a.Albums)
                               .ThenInclude(al => al.Tracks)
                           .ToListAsync();
        }
    }
}
