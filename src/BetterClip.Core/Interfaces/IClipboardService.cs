using System;
using System.Threading.Tasks;
using BetterClip.Core.Models;

namespace BetterClip.Core.Interfaces
{
    public interface IClipboardService
    {
        // Event triggered when a new valid clipboard entry is captured
        event EventHandler<ClipEntry> ClipCaptured;

        // Start listening to Windows clipboard messages
        void StartListening();

        // Stop listening and clean up message hooks
        void StopListening();

        // Temporarily pause clipboard capture
        bool IsPaused { get; set; }
    }
}