using System;
using BetterClip.Core.Interfaces;
using BetterClip.Core.Models;

namespace BetterClip.Services.Clipboard
{
    public class ClipboardListenerService : IClipboardService
    {
        public event EventHandler<ClipEntry>? ClipCaptured;
        public bool IsPaused { get; set; } = false;

        public void StartListening()
        {
            // Hook into WM_CLIPBOARDUPDATE messages here
        }

        public void StopListening()
        {
            // Unhook messages
        }
    }
}