﻿#nullable disable

using MapTo;

namespace ExternalTestData.Models;

[MapFrom(typeof(SpotifyAlbumDto))]
public partial class SpotifyAlbum
{
    public string AlbumType { get; set; }

    public Artist[] Artists { get; set; }

    public string[] AvailableMarkets { get; set; }

    public Copyright[] Copyrights { get; set; }

    public ExternalIds ExternalIds { get; set; }

    public ExternalUrls ExternalUrls { get; set; }

    public object[] Genres { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public Image[] Images { get; set; }

    public string Name { get; set; }

    public long Popularity { get; set; }

    public string ReleaseDate { get; set; }

    public string ReleaseDatePrecision { get; set; }

    public Tracks Tracks { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

[MapFrom(typeof(TracksDto))]
public class Tracks
{
    public string Href { get; set; }

    public Item[] Items { get; set; }

    public long Limit { get; set; }

    public object Next { get; set; }

    public long Offset { get; set; }

    public object Previous { get; set; }

    public long Total { get; set; }
}

[MapFrom(typeof(ItemDto))]
public class Item
{
    public Artist[] Artists { get; set; }

    public string[] AvailableMarkets { get; set; }

    public long DiscNumber { get; set; }

    public long DurationMs { get; set; }

    public bool Explicit { get; set; }

    public ExternalUrls ExternalUrls { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string PreviewUrl { get; set; }

    public long TrackNumber { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

[MapFrom(typeof(ImageDto))]
public class Image
{
    public long Height { get; set; }

    public string Url { get; set; }

    public long Width { get; set; }
}

[MapFrom(typeof(ExternalIdsDto))]
public class ExternalIds
{
    public string Upc { get; set; }
}

[MapFrom(typeof(CopyrightDto))]
public class Copyright
{
    public string Text { get; set; }

    public string Type { get; set; }
}

[MapFrom(typeof(ArtistDto))]
public class Artist
{
    public ExternalUrls ExternalUrls { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

[MapFrom(typeof(ExternalUrlsDto))]
public class ExternalUrls
{
    public string Spotify { get; set; }
}