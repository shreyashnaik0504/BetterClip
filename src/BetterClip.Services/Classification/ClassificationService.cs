using BetterClip.Core.Models;

namespace BetterClip.Services.Classification
{
    public class ClassificationService
    {
        public ContentType DetectType(string content)
        {
            // TODO: Implement heuristic/regex classification for text, URLs, etc.
            return ContentType.Text;
        }
    }
}
