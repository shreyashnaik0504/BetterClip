using System.Collections.Generic;
using System.Threading.Tasks;
using BetterClip.Core.Models;

namespace BetterClip.Core.Interfaces
{
    public interface ISearchService
    {
        Task<List<ClipEntry>> SearchAsync(string query, string? categoryFilter = null);
        Task IndexClipAsync(ClipEntry entry);
        Task RemoveIndexAsync(string id);
    }
}