using BetterClip.Core.Models;

namespace BetterClip.Services.Privacy
{
    public class PrivacyService
    {
        public bool IsExcluded(string processName, AppSettings settings)
        {
            // TODO: Check if process matches password managers or excluded apps
            return false;
        }
    }
}
