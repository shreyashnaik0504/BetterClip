using System.Collections.Generic;
using System.Threading.Tasks;
using BetterClip.Core.Models;

namespace BetterClip.Core.Interfaces
{
    public interface IStorageService
    {
        Task InitializeDatabaseAsync();
        Task SaveClipAsync(ClipEntry entry);
        Task<List<ClipEntry>> GetAllClipsAsync();
        Task<List<ClipEntry>> GetClipsByCategoryAsync(string category);
        Task TogglePinAsync(string id);
        Task DeleteClipAsync(string id);
        Task ClearAllAsync(bool includePinned = false);
    }
}