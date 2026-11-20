
namespace RaceDay.API.Models;

public class EventMedia
{
    public int EventMediaID { get; set; }

    public int EventID { get; set; }

    public string BlobName { get; set; } = "";

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
}
