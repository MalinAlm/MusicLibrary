using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicLibrary.Services
{
    class MusicService
    {
        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            using var db = new MusicContext();
            return await db.Playlists.ToListAsync();
        }

        public async Task<List<Track>> GetTracksAsync()
        {
            using var db = new MusicContext();
            return await db.Tracks.ToListAsync();
        }
    }
}
