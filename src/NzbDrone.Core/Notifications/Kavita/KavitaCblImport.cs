using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Kavita;

// Wire DTOs for Kavita's CBL import endpoints. Enums come over the wire as
// numbers (Kavita doesn't register a string enum converter), so they're
// plain ints here; values are shared between v0.8 and v0.9.
public class KavitaCblSavedFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("provider")]
    public int Provider { get; set; }
}

public class KavitaCblImportSummary
{
    public const int ResultFail = 0;
    public const int ResultPartial = 1;
    public const int ResultSuccess = 2;

    [JsonPropertyName("cblName")]
    public string CblName { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("results")]
    public List<KavitaCblBookResult> Results { get; set; } = new List<KavitaCblBookResult>();

    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("successfulInserts")]
    public List<KavitaCblBookResult> SuccessfulInserts { get; set; } = new List<KavitaCblBookResult>();

    [JsonPropertyName("isUpdate")]
    public bool IsUpdate { get; set; }

    [JsonPropertyName("readingListId")]
    public int ReadingListId { get; set; }
}

public class KavitaCblBookResult
{
    public const int ReasonSuccess = 8;

    private static readonly Dictionary<int, string> ReasonDescriptions = new Dictionary<int, string>
    {
        { 0, "chapter missing" },
        { 1, "volume missing" },
        { 2, "series missing" },
        { 3, "name conflict" },
        { 4, "all series missing" },
        { 5, "empty file" },
        { 6, "series collision" },
        { 7, "all chapters missing" },
        { 8, "success" },
        { 9, "invalid file" }
    };

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("series")]
    public string Series { get; set; }

    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("reason")]
    public int Reason { get; set; }

    public string Describe()
    {
        var reason = ReasonDescriptions.TryGetValue(Reason, out var description) ? description : $"reason {Reason}";

        if (string.IsNullOrWhiteSpace(Series))
        {
            return reason;
        }

        return string.IsNullOrWhiteSpace(Number) ? $"{Series}: {reason}" : $"{Series} #{Number}: {reason}";
    }
}
