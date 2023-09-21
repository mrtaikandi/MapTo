using MappingLibrariesBenchmark.Mappings;
using MappingLibrariesBenchmark.Models;

namespace MappingLibrariesBenchmark.Scenarios;

[MemoryDiagnoser]
[KeepBenchmarkFiles(false)]
[SimpleJob(RunStrategy.Throughput)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
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