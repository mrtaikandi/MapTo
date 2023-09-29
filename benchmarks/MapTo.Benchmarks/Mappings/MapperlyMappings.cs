using MapTo.Benchmarks.Models;
using Riok.Mapperly.Abstractions;

namespace MapTo.Benchmarks.Mappings;

[Mapper]
public partial class MapperlyMappings
{
    public static MapperlyMappings Configure() => new();

    public partial SpotifyAlbum Map(SpotifyAlbumDto spotifyAlbumDto);
}