using System;
using System.Collections.ObjectModel;

namespace BetterClip.UI.ViewModels
{
    // Temporary self-contained clip model to eliminate the reference break immediately
    public class ClipItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = "Text";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsPinned { get; set; } = false;
    }

    public class FloatingPanelViewModel
    {
        public ObservableCollection<ClipItem> Clips { get; set; } = new();
        public string SearchQuery { get; set; } = string.Empty;
        public string SelectedCategory { get; set; } = "All";
    }
}