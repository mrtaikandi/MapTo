using MapTo.Benchmarks.Mappings;
using MapTo.Benchmarks.Models;

namespace MapTo.Benchmarks.Scenarios;

[InProcess]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BenchmarkMapToAndMapperly
{
    private readonly MapperlyMappings _mapperlyMappings;
    private readonly SpotifyAlbumDto _spotifyAlbumDto;

    public BenchmarkMapToAndMapperly()
    {
        var json = File.ReadAllText("Data\\spotifyAlbum.json");

        _spotifyAlbumDto = SpotifyAlbumDto.FromJson(json);
        _mapperlyMappings = MapperlyMappings.Configure();
    }

    [Benchmark]
    public SpotifyAlbum MapTo()
    {
        return _spotifyAlbumDto.MapToSpotifyAlbum();
    }

    [Benchmark]
    public SpotifyAlbum Mapperly()
    {
        return _mapperlyMappings.Map(_spotifyAlbumDto);
    }
}