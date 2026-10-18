using System;
using System.Collections.Generic;

namespace BetterClip.Core.Models
{
    public enum ContentType
    {
        Text,
        Image,
        Link,
        File,
        Video
    }

    public class ClipEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ContentType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Metadata { get; set; } = string.Empty;
        public string? LocalMediaFilePath { get; set; }
        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsPinned { get; set; } = false;
        public string SourceApplication { get; set; } = string.Empty;
        public HashSet<string> Categories { get; set; } = new();
    }
}
