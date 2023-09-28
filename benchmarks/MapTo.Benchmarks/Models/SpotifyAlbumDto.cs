#nullable disable

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace MapTo.Benchmarks.Models;

public partial class SpotifyAlbumDto
{
    [JsonProperty("album_type")]
    public string AlbumType { get; set; }

    [JsonProperty("artists")]
    public ArtistDto[] Artists { get; set; }

    [JsonProperty("available_markets")]
    public string[] AvailableMarkets { get; set; }

    [JsonProperty("copyrights")]
    public CopyrightDto[] Copyrights { get; set; }

    [JsonProperty("external_ids")]
    public ExternalIdsDto ExternalIds { get; set; }

    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    [JsonProperty("genres")]
    public object[] Genres { get; set; }

    [JsonProperty("href")]
    public string Href { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("images")]
    public ImageDto[] Images { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("popularity")]
    public long Popularity { get; set; }

    [JsonProperty("release_date")]
    public string ReleaseDate { get; set; }

    [JsonProperty("release_date_precision")]
    public string ReleaseDatePrecision { get; set; }

    [JsonProperty("tracks")]
    public TracksDto Tracks { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }
}

public class TracksDto
{
    [JsonProperty("href")]
    public string Href { get; set; }

    [JsonProperty("items")]
    public ItemDto[] Items { get; set; }

    [JsonProperty("limit")]
    public long Limit { get; set; }

    [JsonProperty("next")]
    public object Next { get; set; }

    [JsonProperty("offset")]
    public long Offset { get; set; }

    [JsonProperty("previous")]
    public object Previous { get; set; }

    [JsonProperty("total")]
    public long Total { get; set; }
}

public class ItemDto
{
    [JsonProperty("artists")]
    public ArtistDto[] Artists { get; set; }

    [JsonProperty("available_markets")]
    public string[] AvailableMarkets { get; set; }

    [JsonProperty("disc_number")]
    public long DiscNumber { get; set; }

    [JsonProperty("duration_ms")]
    public long DurationMs { get; set; }

    [JsonProperty("explicit")]
    public bool Explicit { get; set; }

    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    [JsonProperty("href")]
    public string Href { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("preview_url")]
    public string PreviewUrl { get; set; }

    [JsonProperty("track_number")]
    public long TrackNumber { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }
}

public class ImageDto
{
    [JsonProperty("height")]
    public long Height { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("width")]
    public long Width { get; set; }
}

public class ExternalIdsDto
{
    [JsonProperty("upc")]
    public string Upc { get; set; }
}

public class CopyrightDto
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

public class ArtistDto
{
    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    [JsonProperty("href")]
    public string Href { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }
}

public class ExternalUrlsDto
{
    [JsonProperty("spotify")]
    public string Spotify { get; set; }
}

public partial class SpotifyAlbumDto
{
    public static SpotifyAlbumDto FromJson(string json) => JsonConvert.DeserializeObject<SpotifyAlbumDto>(json, Converter.Settings);
}

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
public static class Serialize
{
    public static string ToJson(this SpotifyAlbumDto self) => JsonConvert.SerializeObject(self, Converter.Settings);
}

public class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None
    };
}