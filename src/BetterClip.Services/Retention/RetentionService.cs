using System.Threading.Tasks;
using BetterClip.Core.Interfaces;

namespace BetterClip.Services.Retention
{
    public class RetentionService : IRetentionService
    {
        public Task EnforceRetentionPolicyAsync() => Task.CompletedTask;

        public void StartPeriodicCleanup()
        {
            // TODO: Initialize background timer for hourly cleanup checks
        }

        public void StopPeriodicCleanup() { }
    }
}
