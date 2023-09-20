using MappingLibrariesBenchmark.Models;
using Riok.Mapperly.Abstractions;

namespace MappingLibrariesBenchmark.Mappings;

[Mapper]
public partial class MapperlyMappings
{
    public static MapperlyMappings Configure() => new();

    public partial SpotifyAlbum Map(SpotifyAlbumDto spotifyAlbumDto);
}