#nullable disable

namespace ExternalTestData.Models;

public class SpotifyAlbumDto
{
    public string AlbumType { get; set; }

    public ArtistDto[] Artists { get; set; }

    public string[] AvailableMarkets { get; set; }

    public CopyrightDto[] Copyrights { get; set; }

    public ExternalIdsDto ExternalIds { get; set; }

    public ExternalUrlsDto ExternalUrls { get; set; }

    public object[] Genres { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public ImageDto[] Images { get; set; }

    public string Name { get; set; }

    public long Popularity { get; set; }

    public string ReleaseDate { get; set; }

    public string ReleaseDatePrecision { get; set; }

    public TracksDto Tracks { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

public class TracksDto
{
    public string Href { get; set; }

    public ItemDto[] Items { get; set; }

    public long Limit { get; set; }

    public object Next { get; set; }

    public long Offset { get; set; }

    public object Previous { get; set; }

    public long Total { get; set; }
}

public class ItemDto
{
    public ArtistDto[] Artists { get; set; }

    public string[] AvailableMarkets { get; set; }

    public long DiscNumber { get; set; }

    public long DurationMs { get; set; }

    public bool Explicit { get; set; }

    public ExternalUrlsDto ExternalUrls { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string PreviewUrl { get; set; }

    public long TrackNumber { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

public class ImageDto
{
    public long Height { get; set; }

    public string Url { get; set; }

    public long Width { get; set; }
}

public class ExternalIdsDto
{
    public string Upc { get; set; }
}

public class CopyrightDto
{
    public string Text { get; set; }

    public string Type { get; set; }
}

public class ArtistDto
{
    public ExternalUrlsDto ExternalUrls { get; set; }

    public string Href { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public string Uri { get; set; }
}

public class ExternalUrlsDto
{
    public string Spotify { get; set; }
}