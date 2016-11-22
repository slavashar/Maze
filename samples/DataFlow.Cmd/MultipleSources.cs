using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Maze;

namespace DataFlow.Cmd
{
    public class MultipleSources
    {
        public static void Run()
        {
            var sourceAlbum = Engine.Source("Album source", ChinookContext.Get(x => x.Album, 5));

            var sourceArtist = Engine.Source("Artist source", ChinookContext.Get(x => x.Artist, 10));

            var sourceTraks = Engine.Source("Track source", ChinookContext.Get(x => x.Track, 1));

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
