using System.Collections.Generic;
using System.Threading.Tasks;
using BetterClip.Core.Interfaces;
using BetterClip.Core.Models;

namespace BetterClip.Services.Search
{
    public class Fts5SearchService : ISearchService
    {
        public Task<List<ClipEntry>> SearchAsync(string query, string? categoryFilter = null) 
            => Task.FromResult(new List<ClipEntry>());

        public Task IndexClipAsync(ClipEntry entry) => Task.CompletedTask;

        public Task RemoveIndexAsync(string id) => Task.CompletedTask;
    }
}
