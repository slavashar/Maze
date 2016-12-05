using CommandLine;
using Maze;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace DataFlow.Cmd
{
    [Verb("multiple", HelpText = "Execute a mapping with multiple source.")]
    public class MultipleSourcesOptions : IExample
    {
        public void Execute()
        {
        }

        public void Execute(ChinookContext context)
        {
            var sourceAlbum = Engine.Source("Album source", context.Album);
            var sourceArtist = Engine.Source("Artist source", context.Artist);
            var sourceTraks = Engine.Source("Track source", context.Track);

            var albumName = Engine.Map(
                (IQueryable<Album> albums, IQueryable<Artist> artists) =>
                    from artist in artists
                    join album in albums on artist.ArtistId equals album.ArtistId
                    select album.Title + " form " + artist.Name);

            var trackName = Engine.Map(
                (IQueryable<Album> albums, IQueryable<Track> tracks) =>
                    from album in albums
                    join track in tracks on album.AlbumId equals track.AlbumId
                    select track.Name + " form " + album.Title);

            var execution = Engine
                .Combine(sourceAlbum, sourceArtist, sourceTraks, albumName, trackName)
                .Build();

            execution.GetStream(sourceAlbum).Buffer(30).Subscribe(x => Console.WriteLine("Received " + x.Count + " albums"));

            execution.GetStream(albumName).Buffer(50).Subscribe(x => Console.WriteLine("Received " + x.Count + " album names"));

            execution.GetStream(trackName).Buffer(200).Subscribe(x => Console.WriteLine("Received " + x.Count + " track names"));

            execution.Release().Wait();
        }
    }
}
