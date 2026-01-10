using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MusicLibrary;

public partial class MusicContext : DbContext
{
    public MusicContext()
    {
    }

    public MusicContext(DbContextOptions<MusicContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Album> Albums { get; set; }

    public virtual DbSet<Artist> Artists { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<MediaType> MediaTypes { get; set; }

    public virtual DbSet<Playlist> Playlists { get; set; }

    public virtual DbSet<PlaylistTrack> PlaylistTracks { get; set; }

    public virtual DbSet<Track> Tracks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddUserSecrets<MusicContext>().Build();
        var connectionString = config["ConnectionString"];
        optionsBuilder.UseSqlServer(connectionString);
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("albums", "music");

            entity.Property(e => e.AlbumId).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(160);

            entity.HasOne(d => d.Artist).WithMany(p => p.Albums)
                .HasForeignKey(d => d.ArtistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_albums_artists");
        });

        modelBuilder.Entity<Artist>(entity =>
        {
            entity.ToTable("artists", "music");

            entity.Property(e => e.ArtistId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("genres", "music");

            entity.Property(e => e.GenreId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<MediaType>(entity =>
        {
            entity.ToTable("media_types", "music");

            entity.Property(e => e.MediaTypeId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.ToTable("playlists", "music");

            entity.Property(e => e.PlaylistId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<PlaylistTrack>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("playlist_track", "music");

            entity.HasOne(d => d.Playlist).WithMany()
                .HasForeignKey(d => d.PlaylistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_playlist_track_playlists");

            entity.HasOne(d => d.Track).WithMany()
                .HasForeignKey(d => d.TrackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_playlist_track_tracks");
        });

        modelBuilder.Entity<Track>(entity =>
        {
            entity.ToTable("tracks", "music");

            entity.Property(e => e.TrackId).ValueGeneratedNever();
            entity.Property(e => e.Composer).HasMaxLength(220);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Album).WithMany(p => p.Tracks)
                .HasForeignKey(d => d.AlbumId)
                .HasConstraintName("FK_tracks_albums");

            entity.HasOne(d => d.Genre).WithMany(p => p.Tracks)
                .HasForeignKey(d => d.GenreId)
                .HasConstraintName("FK_tracks_genres");

            entity.HasOne(d => d.MediaType).WithMany(p => p.Tracks)
                .HasForeignKey(d => d.MediaTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tracks_media_types");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
