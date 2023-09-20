using AutoMapper;
using MappingLibrariesBenchmark.Models;

namespace MappingLibrariesBenchmark.Mappings;

public class AutomapperMappings
{
    public static IMapper Configure()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SpotifyAlbumDto, SpotifyAlbum>();
            cfg.CreateMap<CopyrightDto, Copyright>();
            cfg.CreateMap<ArtistDto, Artist>();
            cfg.CreateMap<ExternalIdsDto, ExternalIds>();
            cfg.CreateMap<ExternalUrlsDto, ExternalUrls>();
            cfg.CreateMap<TracksDto, Tracks>();
            cfg.CreateMap<ImageDto, Image>();
            cfg.CreateMap<ItemDto, Item>();
            cfg.CreateMap<SpotifyAlbum, SpotifyAlbumDto>();
            cfg.CreateMap<Copyright, CopyrightDto>();
            cfg.CreateMap<Artist, ArtistDto>();
            cfg.CreateMap<ExternalIds, ExternalIdsDto>();
            cfg.CreateMap<ExternalUrls, ExternalUrlsDto>();
            cfg.CreateMap<Tracks, TracksDto>();
            cfg.CreateMap<Image, ImageDto>();
            cfg.CreateMap<Item, ItemDto>();
        });

        return mapperConfig.CreateMapper();
    }
}