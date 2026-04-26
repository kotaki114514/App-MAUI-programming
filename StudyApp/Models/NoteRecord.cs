using System;
using System.Text.Json.Serialization;

namespace StudyApp.Models;

/// <summary>
/// Represents a user note record.
/// </summary>
public sealed class NoteRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    [JsonIgnore]
    public string ContentPreview
    {
        get
        {
            var trimmed = Content.Replace("\r", " ").Replace("\n", " ").Trim();
            return trimmed.Length <= 82 ? trimmed : $"{trimmed[..82]}...";
        }
    }
}
