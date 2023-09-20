using ExpressMapper;
using MappingLibrariesBenchmark.Models;

namespace MappingLibrariesBenchmark.Mappings;

public class ExpressMapperMappings
{
    public static void Configure()
    {
        Mapper.Register<SpotifyAlbumDto, SpotifyAlbum>();
        Mapper.Register<CopyrightDto, Copyright>();
        Mapper.Register<ArtistDto, Artist>();
        Mapper.Register<ExternalIdsDto, ExternalIds>();
        Mapper.Register<ExternalUrlsDto, ExternalUrls>();
        Mapper.Register<TracksDto, Tracks>();
        Mapper.Register<ImageDto, Image>();
        Mapper.Register<ItemDto, Item>();
        Mapper.Register<SpotifyAlbum, SpotifyAlbumDto>();
        Mapper.Register<Copyright, CopyrightDto>();
        Mapper.Register<Artist, ArtistDto>();
        Mapper.Register<ExternalIds, ExternalIdsDto>();
        Mapper.Register<ExternalUrls, ExternalUrlsDto>();
        Mapper.Register<Tracks, TracksDto>();
        Mapper.Register<Image, ImageDto>();
        Mapper.Register<Item, ItemDto>();
    }
}