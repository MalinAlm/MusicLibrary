using Microsoft.EntityFrameworkCore;
using MusicLibrary;

namespace MusicLibrary.Services
{
    public class MusicService
    {
       
        public async Task<List<Track>> GetTracksAsync()
        {
            using var db = new MusicContext();
            return await db.Tracks
                           .Include(t => t.Album)
                           .ThenInclude(a => a.Artist)
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

        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            using var db = new MusicContext();
            return await db.Playlists
                           .OrderBy(p => p.Name)
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

        public async Task<Playlist> CreatePlaylistAsync(string name)
        {
            using var db = new MusicContext();

            var nextId = await db.Playlists.MaxAsync(p => p.PlaylistId) + 1;

            var playlist = new Playlist
            {
                PlaylistId = nextId,
                Name = name
            };

            db.Playlists.Add(playlist);
            await db.SaveChangesAsync();

            return playlist;
        }

        public async Task AddTrackToPlaylistAsync(int playlistId, int trackId)
        {
            using var db = new MusicContext();

            var exists = await db.PlaylistTracks
                .AnyAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

            if (exists)
                return;

            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO music.playlist_track (PlaylistId, TrackId) VALUES (@p0, @p1)",
                playlistId,
                trackId
            );
        }

        public async Task UpdatePlaylistNameAsync(int playlistId, string newName)
        {
            using var db = new MusicContext();

            var playlist = await db.Playlists
                .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);

            if (playlist == null)
                return;

            playlist.Name = newName;
            await db.SaveChangesAsync();
        }

        public async Task<List<Track>> GetTracksForPlaylistPagedAsync(
            int playlistId,
            int skip,
            int take)
        {
            using var db = new MusicContext();

            return await db.PlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .Include(pt => pt.Track)
                    .ThenInclude(t => t.Album)
                .Select(pt => pt.Track)
                .OrderBy(t => t.TrackId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

    }
}
