using System.Linq;
using DataFlow.Data;
using Maze;
using Maze.Mappings;

namespace DataFlow
{
    public class MultipleSourcesTransform
    {
        private readonly IQueryable<Artist> artists;
        private readonly IQueryable<Album> albums;
        private readonly IQueryable<Track> tracks;

        public MultipleSourcesTransform(IQueryable<Artist> artists, IQueryable<Album> albums, IQueryable<Track> tracks)
        {
            this.artists = artists;
            this.albums = albums;
            this.tracks = tracks;
        }

        public IQueryable<string> AlbumNames => from artist in this.artists
                                                join album in this.albums on artist.ArtistId equals album.ArtistId
                                                select album.Title + " form " + artist.Name;

        public IQueryable<string> TrackNames => from album in this.albums
                                                join track in this.tracks on album.AlbumId equals track.AlbumId
                                                select track.Name + " form " + album.Title;

        public static ComponentMappingReference<MultipleSourcesTransform> Example()
        {
            return Engine
                .Combine(
                    Engine.Source("Artists", new Artist[0]),
                    Engine.Source("Albums", new Album[0]),
                    Engine.Source("Tracks", new Track[0])
                )
                .CreateComponent<MultipleSourcesTransform>();
        }
    }
}
