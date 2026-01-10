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

        public async Task<Track> CreateTrackAsync(
            string name,
            int milliseconds,
            int mediaTypeId,
            int? albumId,
            int? genreId,
            string? composer)
        {
            using var db = new MusicContext();

            var nextId = ((await db.Tracks.MaxAsync(t => (int?)t.TrackId)) ?? 0) + 1;


            var track = new Track
            {
                TrackId = nextId,
                Name = name,
                Milliseconds = milliseconds,
                MediaTypeId = mediaTypeId,
                AlbumId = albumId,
                GenreId = genreId,
                Composer = composer
            };

            db.Tracks.Add(track);
            await db.SaveChangesAsync();
            return track;
        }

        public async Task UpdateTrackAsync(
            int trackId,
            string name,
            int milliseconds,
            int mediaTypeId,
            int? albumId,
            int? genreId,
            string? composer)
        {
            using var db = new MusicContext();

            var track = await db.Tracks.FindAsync(trackId);
            if (track == null) return;

            track.Name = name;
            track.Milliseconds = milliseconds;
            track.MediaTypeId = mediaTypeId;
            track.AlbumId = albumId;
            track.GenreId = genreId;
            track.Composer = composer;

            await db.SaveChangesAsync();
        }

        public async Task DeleteTrackAsync(int trackId)
        {
            using var db = new MusicContext();

            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM music.playlist_track WHERE TrackId = @p0",
                trackId
            );

            var track = await db.Tracks.FirstOrDefaultAsync(t => t.TrackId == trackId);
            if (track == null) return;

            db.Tracks.Remove(track);
            await db.SaveChangesAsync();
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

        public async Task<List<Artist>> GetArtistsTreeAsync()
        {
            using var db = new MusicContext();

            return await db.Artists
                .AsNoTracking()
                .Include(a => a.Albums)
                    .ThenInclude(al => al.Tracks)
                .OrderBy(a => a.Name ?? "")
                .ToListAsync();
        }


        public async Task<Artist> CreateArtistAsync(string name)
        {
            using var db = new MusicContext();

            var trimmed = name.Trim();

            var existing = await db.Artists
                .FirstOrDefaultAsync(a => a.Name != null && a.Name.ToLower() == trimmed.ToLower());

            if (existing != null)
                return existing;

            var maxId = await db.Artists.MaxAsync(a => (int?)a.ArtistId);
            var nextId = (maxId ?? 0) + 1;

            var artist = new Artist { ArtistId = nextId, Name = trimmed };
            db.Artists.Add(artist);

            await db.SaveChangesAsync();
            return artist;
        }


        public async Task UpdateArtistAsync(int artistId, string newName)
        {
            using var db = new MusicContext();
            var artist = await db.Artists.FindAsync(artistId);
            if (artist == null) return;

            artist.Name = newName;
            await db.SaveChangesAsync();
        }

        public async Task DeleteArtistAsync(int artistId)
        {
            using var db = new MusicContext();

            var artist = await db.Artists
                .Include(a => a.Albums)
                .FirstOrDefaultAsync(a => a.ArtistId == artistId);

            if (artist == null) return;

            if (artist.Albums.Any())
                throw new InvalidOperationException("Artisten har album kopplade och kan inte tas bort.");

            db.Artists.Remove(artist);
            await db.SaveChangesAsync();
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

        public async Task<List<MediaType>> GetMediaTypesAsync()
        {
            using var db = new MusicContext();
            return await db.MediaTypes
                           .OrderBy(m => m.Name)
                           .ToListAsync();
        }


        public async Task<Playlist> CreatePlaylistAsync(string name)
        {
            using var db = new MusicContext();
            var nextId = ((await db.Playlists.MaxAsync(t => (int?)t.PlaylistId)) ?? 0) + 1;


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

        public async Task RemoveTrackFromPlaylistAsync(int playlistId, int trackId)
        {
            using var db = new MusicContext();

            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM music.playlist_track WHERE PlaylistId = @p0 AND TrackId = @p1",
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

        public async Task DeletePlaylistAsync(int playlistId)
        {
            using var db = new MusicContext();

            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM music.playlist_track WHERE PlaylistId = @p0",
                playlistId
            );

            var playlist = await db.Playlists.FirstOrDefaultAsync(p => p.PlaylistId == playlistId);
            if (playlist == null) return;

            db.Playlists.Remove(playlist);
            await db.SaveChangesAsync();
        }

        public async Task<List<Track>> GetTracksPageAsync(
        int numberOfTracksToSkip,
        int numberOfTracksToTake,
        string? searchText = null)
        {
            using var db = new MusicContext();

            IQueryable<Track> trackQuery = db.Tracks
                .AsNoTracking()
                .Include(track => track.Album)
                    .ThenInclude(album => album.Artist);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string trimmedSearchText = searchText.Trim();
                trackQuery = trackQuery.Where(track => track.Name.Contains(trimmedSearchText));
            }

            return await trackQuery
                .OrderBy(track => track.TrackId)
                .Skip(numberOfTracksToSkip)
                .Take(numberOfTracksToTake)
                .ToListAsync();
        }


        public async Task<List<Artist>> GetArtistsPageAsync(
            int numberOfArtistsToSkip,
            int numberOfArtistsToTake,
            string? searchText = null)
        {
            using var db = new MusicContext();

            IQueryable<Artist> artistQuery = db.Artists.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string trimmedSearchText = searchText.Trim();

                artistQuery = artistQuery.Where(artist =>
                    artist.Name != null && artist.Name.Contains(trimmedSearchText));
            }

            return await artistQuery
                .OrderBy(artist => artist.ArtistId)
                .Skip(numberOfArtistsToSkip)
                .Take(numberOfArtistsToTake)
                .ToListAsync();
        }

        public async Task<Album> CreateAlbumAsync(string title, int artistId)
        {
            using var db = new MusicContext();

            var maxId = await db.Albums.MaxAsync(a => (int?)a.AlbumId);
            var nextId = (maxId ?? 0) + 1;

            var album = new Album
            {
                AlbumId = nextId,
                Title = title,
                ArtistId = artistId
            };

            db.Albums.Add(album);
            await db.SaveChangesAsync();
            return album;
        }


        public async Task UpdateAlbumAsync(int albumId, string newTitle, int artistId)
        {
            using var db = new MusicContext();

            var album = await db.Albums.FindAsync(albumId);
            if (album == null) return;

            album.Title = newTitle;
            album.ArtistId = artistId;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAlbumAsync(int albumId)
        {
            using var db = new MusicContext();

            var album = await db.Albums
                .Include(a => a.Tracks)
                .FirstOrDefaultAsync(a => a.AlbumId == albumId);

            if (album == null) return;

            if (album.Tracks.Any())
                throw new InvalidOperationException("Albumet har låtar kopplade och kan inte tas bort.");

            db.Albums.Remove(album);
            await db.SaveChangesAsync();
        }

    }

}
