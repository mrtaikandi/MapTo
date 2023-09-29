﻿using MapTo.Benchmarks.Models;

namespace MapTo.Benchmarks.Mappings;

public static class ManualMappings
{
    public static SpotifyAlbum Map(this SpotifyAlbumDto spotifyAlbumDto)
    {
        return new SpotifyAlbum
        {
            AlbumType = spotifyAlbumDto.AlbumType,
            Artists = spotifyAlbumDto.Artists.Select(spotifyAlbumDtoArtist => new Artist
            {
                ExternalUrls = new ExternalUrls
                {
                    Spotify = spotifyAlbumDtoArtist.ExternalUrls.Spotify
                },
                Href = spotifyAlbumDtoArtist.Href,
                Id = spotifyAlbumDtoArtist.Id,
                Name = spotifyAlbumDtoArtist.Name,
                Type = spotifyAlbumDtoArtist.Type,
                Uri = spotifyAlbumDtoArtist.Uri
            }).ToArray(),
            AvailableMarkets = spotifyAlbumDto.AvailableMarkets,
            Copyrights = spotifyAlbumDto.Copyrights.Select(spotifyAlbumDtoCopyright => new Copyright
            {
                Text = spotifyAlbumDtoCopyright.Text,
                Type = spotifyAlbumDtoCopyright.Type
            }).ToArray(),
            ExternalIds = new ExternalIds
            {
                Upc = spotifyAlbumDto.ExternalIds.Upc
            },
            ExternalUrls = new ExternalUrls
            {
                Spotify = spotifyAlbumDto.ExternalUrls.Spotify
            },
            Genres = spotifyAlbumDto.Genres,
            Href = spotifyAlbumDto.Href,
            Id = spotifyAlbumDto.Id,
            Images = spotifyAlbumDto.Images.Select(spotifyAlbumDtoImage => new Image
            {
                Height = spotifyAlbumDtoImage.Height,
                Url = spotifyAlbumDtoImage.Url,
                Width = spotifyAlbumDtoImage.Width
            }).ToArray(),
            Name = spotifyAlbumDto.Name,
            Popularity = spotifyAlbumDto.Popularity,
            ReleaseDate = spotifyAlbumDto.ReleaseDate,
            ReleaseDatePrecision = spotifyAlbumDto.ReleaseDatePrecision,
            Tracks = new Tracks
            {
                Href = spotifyAlbumDto.Tracks.Href,
                Items = spotifyAlbumDto.Tracks.Items.Select(spotifyAlbumDtoTracksItem => new Item
                {
                    Artists = spotifyAlbumDtoTracksItem.Artists.Select(spotifyAlbumDtoTracksItemArtist => new Artist
                    {
                        ExternalUrls = new ExternalUrls
                        {
                            Spotify = spotifyAlbumDtoTracksItemArtist.ExternalUrls.Spotify
                        },
                        Href = spotifyAlbumDtoTracksItemArtist.Href,
                        Id = spotifyAlbumDtoTracksItemArtist.Id,
                        Name = spotifyAlbumDtoTracksItemArtist.Name,
                        Type = spotifyAlbumDtoTracksItemArtist.Type,
                        Uri = spotifyAlbumDtoTracksItemArtist.Uri
                    }).ToArray(),
                    AvailableMarkets = spotifyAlbumDtoTracksItem.AvailableMarkets,
                    DiscNumber = spotifyAlbumDtoTracksItem.DiscNumber,
                    DurationMs = spotifyAlbumDtoTracksItem.DurationMs,
                    Explicit = spotifyAlbumDtoTracksItem.Explicit,
                    ExternalUrls = new ExternalUrls
                    {
                        Spotify = spotifyAlbumDtoTracksItem.ExternalUrls.Spotify
                    },
                    Href = spotifyAlbumDtoTracksItem.Href,
                    Id = spotifyAlbumDtoTracksItem.Id,
                    Name = spotifyAlbumDtoTracksItem.Name,
                    PreviewUrl = spotifyAlbumDtoTracksItem.PreviewUrl,
                    TrackNumber = spotifyAlbumDtoTracksItem.TrackNumber,
                    Type = spotifyAlbumDtoTracksItem.Type,
                    Uri = spotifyAlbumDtoTracksItem.Uri
                }).ToArray(),
                Limit = spotifyAlbumDto.Tracks.Limit,
                Next = spotifyAlbumDto.Tracks.Next,
                Offset = spotifyAlbumDto.Tracks.Offset,
                Previous = spotifyAlbumDto.Tracks.Previous,
                Total = spotifyAlbumDto.Tracks.Total
            },
            Type = spotifyAlbumDto.Type,
            Uri = spotifyAlbumDto.Uri
        };
    }
}