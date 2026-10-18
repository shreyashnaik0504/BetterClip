using System.Threading.Tasks;

namespace BetterClip.Core.Interfaces
{
    public interface IRetentionService
    {
        Task EnforceRetentionPolicyAsync();
        void StartPeriodicCleanup();
        void StopPeriodicCleanup();
    }
}