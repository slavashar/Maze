using System;
using System.Collections.Generic;
using System.Linq;
using DataFlow.Data;
using Microsoft.EntityFrameworkCore;


namespace DataFlow.Infrastucture
{
    public class ChinookContext : DbContext
    {
        public ChinookContext(DbContextOptions<ChinookContext> options)
            : base(options)
        {
        }

        public DbSet<Album> Album { get; set; }
        public DbSet<Artist> Artist { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Genre> Genre { get; set; }
        public DbSet<Invoice> Invoice { get; set; }
        public DbSet<InvoiceLine> InvoiceLine { get; set; }
        public DbSet<MediaType> MediaType { get; set; }
        public DbSet<Playlist> Playlist { get; set; }
        public DbSet<PlaylistTrack> PlaylistTrack { get; set; }
        public DbSet<Track> Track { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlaylistTrack>()
                .HasKey(x => new { x.PlaylistId, x.TrackId });
        }
    }
}
