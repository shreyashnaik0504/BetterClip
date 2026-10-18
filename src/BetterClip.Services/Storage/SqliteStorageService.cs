using System.Collections.Generic;
using System.Threading.Tasks;
using BetterClip.Core.Interfaces;
using BetterClip.Core.Models;

namespace BetterClip.Services.Storage
{
    public class SqliteStorageService : IStorageService
    {
        public Task InitializeDatabaseAsync() => Task.CompletedTask;

        public Task SaveClipAsync(ClipEntry entry) => Task.CompletedTask;

        public Task<List<ClipEntry>> GetAllClipsAsync() => Task.FromResult(new List<ClipEntry>());

        public Task<List<ClipEntry>> GetClipsByCategoryAsync(string category) => Task.FromResult(new List<ClipEntry>());

        public Task TogglePinAsync(string id) => Task.CompletedTask;

        public Task DeleteClipAsync(string id) => Task.CompletedTask;

        public Task ClearAllAsync(bool includePinned = false) => Task.CompletedTask;
    }
}