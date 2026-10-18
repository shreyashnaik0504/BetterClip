using System.Collections.Generic;

namespace BetterClip.Core.Models
{
    public class AppSettings
    {
        // Global hotkey combination (e.g., Ctrl + Space)
        public string GlobalShortcutKey { get; set; } = "Ctrl+Space";

        // Theme direction: "Dark" or "Light"
        public string Theme { get; set; } = "Dark";

        // Launch app on Windows startup
        public bool LaunchAtStartup { get; set; } = false;

        // Reduce transparency fallback for accessibility
        public bool ReduceTransparency { get; set; } = false;

        // Maximum storage limit in megabytes before evicting old unpinned items
        public int MaxStorageLimitMb { get; set; } = 500;

        // List of process names or app titles excluded from clipboard capture
        public List<string> ExcludedApplications { get; set; } = new()
        {
            "keepass",
            "1password",
            "bitwarden"
        };
    }
}