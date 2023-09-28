using MapTo.Benchmarks.Models;

namespace MapTo.Benchmarks.Mappings;

public class TinyMapperMappings
{
    public static void Configure()
    {
        Nelibur.ObjectMapper.TinyMapper.Bind<SpotifyAlbumDto, SpotifyAlbum>();
        Nelibur.ObjectMapper.TinyMapper.Bind<CopyrightDto, Copyright>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ArtistDto, Artist>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ExternalIdsDto, ExternalIds>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ExternalUrlsDto, ExternalUrls>();
        Nelibur.ObjectMapper.TinyMapper.Bind<TracksDto, Tracks>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ImageDto, Image>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ItemDto, Item>();
        Nelibur.ObjectMapper.TinyMapper.Bind<SpotifyAlbum, SpotifyAlbumDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<Copyright, CopyrightDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<Artist, ArtistDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ExternalIds, ExternalIdsDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<ExternalUrls, ExternalUrlsDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<Tracks, TracksDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<Image, ImageDto>();
        Nelibur.ObjectMapper.TinyMapper.Bind<Item, ItemDto>();
    }
}